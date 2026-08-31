using EasyNetQ.Consumer;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Benchmarks.Fixtures;

/// <summary>
///     Builds the consume pipeline exactly as <c>RabbitAdvancedBus.ConsumeAsync</c> does for typed handlers:
///     error strategy → interceptors → deserialize (type name + serializer + MessageFactory) → handler lookup → dispatch.
///     The handlers are no-ops returning a cached completed task, so the measured cost is framework overhead only
///     (plus the deserialized message object itself).
/// </summary>
public sealed class ConsumePipelineFixture
{
    private static readonly Task<AckStrategyAsync> AckTask = Task.FromResult(AckStrategies.AckAsync);

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
        ConsumeDelegate = new ConsumePipelineBuilder()
            .UseConsumeErrorStrategy()
            .UseConsumeInterceptors()
            .Use(_ => ctx =>
            {
                var deserializedMessage = strategy.DeserializeMessage(ctx.Properties, ctx.Body);
                var handler = handlerCollection.GetHandler(deserializedMessage.MessageType);
                return new ValueTask<AckStrategyAsync>(handler(deserializedMessage, ctx.ReceivedInfo, ctx.CancellationToken));
            })
            .Build();
    }

    public ConsumeDelegate ConsumeDelegate { get; }
    public IMessageSerializationStrategy SerializationStrategy { get; }
    public IServiceProvider Services { get; }
    public MessageReceivedInfo ReceivedInfo { get; } = new("consumer", 1UL, false, "exchange", "routing.key", "queue");

    public ConsumeContext CreateContext<T>(T message)
    {
        using var serialized = SerializationStrategy.SerializeMessage(new Message<T>(message));
        return new ConsumeContext(ReceivedInfo, serialized.Properties, serialized.Body.ToArray(), Services, CancellationToken.None);
    }
}
