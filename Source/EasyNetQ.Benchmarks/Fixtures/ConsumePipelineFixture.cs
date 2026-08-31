using EasyNetQ.Consumer;
using EasyNetQ.Pipeline;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Benchmarks.Fixtures;

/// <summary>
///     Builds the consume pipeline exactly as <c>RabbitAdvancedBus.ConsumeAsync</c> does for typed handlers
///     (error handling → interceptors → deserialize + handler lookup + dispatch) and drives it the way the transport
///     does: rent a pooled <see cref="ConsumeContext" />, fill it, run the pipeline, return it.
///     The handlers are no-ops, so the measured cost is framework overhead only (plus the deserialized message).
/// </summary>
public sealed class ConsumePipelineFixture
{
    private static readonly ValueTask<AckDecision> AckTask = new(AckDecision.Ack);

    public ConsumePipelineFixture()
    {
        var serializer = new SystemTextJsonSerializerV2();
        var typeNameSerializer = new DefaultTypeNameSerializer();
        SerializationStrategy = new DefaultMessageSerializationStrategy(
            typeNameSerializer, serializer, new DefaultCorrelationIdGenerationStrategy()
        );

        var services = new ServiceCollection();
        services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.Ack);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        Services = services.BuildServiceProvider();

        var handlerCollection = new HandlerCollection();
        handlerCollection.Add<SmallMessage>((_, _, _) => AckTask);
        handlerCollection.Add<MediumMessage>((_, _, _) => AckTask);
        handlerCollection.Add<LargeMessage>((_, _, _) => AckTask);

        var strategy = SerializationStrategy;
        var pipeline = new PipelineBuilder<ConsumeContext>()
            .UseConsumeErrorStrategy()
            .UseConsumeInterceptors()
            .Build(Services, async context =>
            {
                var message = strategy.DeserializeMessage(context.Properties, context.Body);
                var handler = handlerCollection.GetHandler(message.MessageType);
                context.Ack = await handler(message, context.ReceivedInfo, context.CancellationToken).ConfigureAwait(false);
            });

        var connection = new ConnectionContext("Consumer", Services);
        Consumer = new ConsumerContext(new ChannelContext(connection), ReceivedInfo.Queue) { MessagePipeline = pipeline };
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
        using var serialized = SerializationStrategy.SerializeMessage(new Message<T>(message));
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
