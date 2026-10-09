using EasyNetQ.Consumer;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Benchmarks.Fixtures;

/// <summary>
///     Builds the consume pipeline exactly as <c>RabbitAdvancedBus.ConsumeAsync</c> does for typed handlers
///     (error handling → interceptors → resolve type → resolve handler → select serializer → deserialize → dispatch)
///     and drives it the way the transport does: rent a pooled <see cref="ConsumeContext" />, fill it, run the
///     pipeline, return it. Handlers are no-ops, so the measured cost is framework overhead plus the deserialized
///     message object.
/// </summary>
public sealed class ConsumePipelineFixture
{
    private static readonly ValueTask<AckDecision> AckTask = new(AckDecision.Ack);

    public ConsumePipelineFixture()
    {
        var serializer = new SystemTextJsonMessageSerializer();
        var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());
        SerializationStrategy = new DefaultMessageSerializationStrategy(
            registry, serializer, new DefaultCorrelationIdGenerationStrategy()
        );

        var services = new ServiceCollection();
        services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.Ack);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        Services = services.BuildServiceProvider();

        var table = new HandlerTable(registry);
        table.Add<SmallMessage>(static (_, _) => AckTask);
        table.Add<MediumMessage>(static (_, _) => AckTask);
        table.Add<LargeMessage>(static (_, _) => AckTask);

        var pipeline = new PipelineBuilder<ConsumeContext>()
            .UseConsumeErrorStrategy()
            .UseConsumeInterceptors()
            .Use(new ResolveMessageTypeStep())
            .Use(new ResolveHandlerStep())
            .Use(new SelectSerializerStep(serializer))
            .Use(new DeserializeStep())
            .Build(Services, static async context => context.Ack = await context.Handler!.InvokeAsync(context).ConfigureAwait(false));

        var connection = new ConnectionContext("Consumer", Services);
        Consumer = new ConsumerContext(new ChannelContext(connection), ReceivedInfo.Queue)
        {
            Handlers = table,
            MessagePipeline = pipeline,
        };
    }

    public ConsumerContext Consumer { get; }
    public IMessageSerializationStrategy SerializationStrategy { get; }
    public IServiceProvider Services { get; }
    public MessageReceivedInfo ReceivedInfo { get; } = new("consumer", 1UL, false, "exchange", "routing.key", "queue");

    /// <summary>
    ///     Serializes <paramref name="message" /> the way the publish path does, yielding what a consumer would receive
    /// </summary>
    public (MessageProperties Properties, ReadOnlyMemory<byte> Body) Serialize<T>(T message)
    {
        using var serialized = SerializationStrategy.SerializeMessage(message, MessageProperties.Empty);
        return (serialized.Properties, serialized.Body.ToArray());
    }

    /// <summary>
    ///     What <c>AsyncBasicConsumer.HandleBasicDeliverAsync</c> does per delivery, minus the broker ack
    /// </summary>
    public ValueTask ConsumeAsync(in MessageProperties properties, ReadOnlyMemory<byte> body)
    {
        var context = Consumer.RentMessageContext();
        context.ReceivedInfo = ReceivedInfo;
        context.Properties = properties;
        context.Body = body;

        var task = Consumer.MessagePipeline(context);
        if (task.IsCompletedSuccessfully)
        {
            Consumer.ReturnMessageContext(context);
            return default;
        }

        return AwaitAndReturn(task, context);
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
