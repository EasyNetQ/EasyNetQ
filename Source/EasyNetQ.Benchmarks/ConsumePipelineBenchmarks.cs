using BenchmarkDotNet.Attributes;
using EasyNetQ.Consumer;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Benchmarks;

[MemoryDiagnoser]
public class ConsumePipelineBenchmarks
{
    private ConsumeDelegate consumeDelegate = null!;
    private ConsumeContext smallContext;
    private ConsumeContext mediumContext;
    private ConsumeContext largeContext;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var serializer = new SystemTextJsonSerializerV2();
        var typeNameSerializer = new DefaultTypeNameSerializer();
        var messageSerializationStrategy = new DefaultMessageSerializationStrategy(
            typeNameSerializer, serializer, new DefaultCorrelationIdGenerationStrategy()
        );

        // Minimal DI container matching the real consume pipeline dependencies
        var services = new ServiceCollection();
        services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.Ack);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var provider = services.BuildServiceProvider();

        // Handler collection with a no-op typed handler (simulates user handler)
        var handlerCollection = new HandlerCollection();
        handlerCollection.Add<SmallMessage>((_, _, _) => Task.FromResult(AckStrategies.AckAsync));
        handlerCollection.Add<MediumMessage>((_, _, _) => Task.FromResult(AckStrategies.AckAsync));
        handlerCollection.Add<LargeMessage>((_, _, _) => Task.FromResult(AckStrategies.AckAsync));

        // Build the pipeline as RabbitAdvancedBus does:
        // UseConsumeErrorStrategy -> UseConsumeInterceptors -> Deserialize + Dispatch
        consumeDelegate = new ConsumePipelineBuilder()
            .UseConsumeErrorStrategy()
            .UseConsumeInterceptors()
            .Use(_ => ctx =>
            {
                var deserializedMessage = messageSerializationStrategy.DeserializeMessage(ctx.Properties, ctx.Body);
                var handler = handlerCollection.GetHandler(deserializedMessage.MessageType);
                return new ValueTask<AckStrategyAsync>(handler(deserializedMessage, ctx.ReceivedInfo, ctx.CancellationToken));
            })
            .Build();

        var receivedInfo = new MessageReceivedInfo("consumer", 1UL, false, "exchange", "routing.key", "queue");

        smallContext = CreateContext(messageSerializationStrategy, SampleMessages.CreateSmall(), receivedInfo, provider);
        mediumContext = CreateContext(messageSerializationStrategy, SampleMessages.CreateMedium(), receivedInfo, provider);
        largeContext = CreateContext(messageSerializationStrategy, SampleMessages.CreateLarge(), receivedInfo, provider);
    }

    private static ConsumeContext CreateContext<T>(
        IMessageSerializationStrategy strategy, T message, MessageReceivedInfo receivedInfo, IServiceProvider services)
    {
        using var serialized = strategy.SerializeMessage(new Message<T>(message));
        return new ConsumeContext(receivedInfo, serialized.Properties, serialized.Body.ToArray(), services, CancellationToken.None);
    }

    [Benchmark]
    public ValueTask<AckStrategyAsync> Consume_Small() => consumeDelegate(smallContext);

    [Benchmark]
    public ValueTask<AckStrategyAsync> Consume_Medium() => consumeDelegate(mediumContext);

    [Benchmark]
    public ValueTask<AckStrategyAsync> Consume_Large() => consumeDelegate(largeContext);
}
