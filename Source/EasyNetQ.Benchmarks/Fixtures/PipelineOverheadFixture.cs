using EasyNetQ.Consumer;
using EasyNetQ.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Benchmarks.Fixtures;

/// <summary>
///     The consume pipeline plumbing alone: pooled context rent/fill/return plus the default middleware
///     (error handling, interceptors) ending in a no-op terminal. No deserialization, no handler lookup.
///     This is the number that must stay at 0 B.
/// </summary>
public sealed class PipelineOverheadFixture
{
    private static readonly PropertyKey<int> Marker = new("marker");

    public PipelineOverheadFixture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.Ack);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var provider = services.BuildServiceProvider();

        var pipeline = new PipelineBuilder<ConsumeContext>()
            .UseConsumeErrorStrategy()
            .UseConsumeInterceptors()
            .Build(provider, static context =>
            {
                context.Ack = AckDecision.Ack;
                return default;
            });

        Connection = new ConnectionContext("Consumer", provider);
        Connection.Set(Marker, 42);
        Channel = new ChannelContext(Connection);
        Consumer = new ConsumerContext(Channel, "queue") { MessagePipeline = pipeline };
        Properties = new MessageProperties { Type = "type", CorrelationId = "correlation" };
        Body = new byte[] { 1, 2, 3, 4 };
    }

    public ConnectionContext Connection { get; }
    public ChannelContext Channel { get; }
    public ConsumerContext Consumer { get; }
    public MessageProperties Properties { get; }
    public ReadOnlyMemory<byte> Body { get; }
    public MessageReceivedInfo ReceivedInfo { get; } = new("consumer", 1UL, false, "exchange", "routing.key", "queue");

    /// <summary>What the transport does per delivery, with a no-op terminal</summary>
    public ValueTask ConsumeNoopAsync()
    {
        var context = Consumer.RentMessageContext();
        context.ReceivedInfo = ReceivedInfo;
        context.Properties = Properties;
        context.Body = Body;

        var task = Consumer.MessagePipeline(context);
        if (task.IsCompletedSuccessfully)
        {
            Consumer.ReturnMessageContext(context);
            return default;
        }

        return AwaitAndReturn(task, context);
    }

    /// <summary>Reads a value set three layers up (connection) from a message context</summary>
    public int ReadInheritedProperty()
    {
        var context = Consumer.RentMessageContext();
        var value = context.Get(Marker);
        Consumer.ReturnMessageContext(context);
        return value;
    }

    private async ValueTask AwaitAndReturn(ValueTask task, ConsumeContext context)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        finally
        {
            Consumer.ReturnMessageContext(context);
        }
    }
}
