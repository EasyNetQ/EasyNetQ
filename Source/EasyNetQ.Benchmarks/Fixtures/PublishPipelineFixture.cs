using EasyNetQ.Producer;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Benchmarks.Fixtures;

/// <summary>
///     Builds the publish path as it runs inside the process, stopping at the transport boundary:
///     the produce pipeline terminal maps <see cref="MessageProperties" /> onto a fresh <see cref="BasicProperties" />
///     (as <c>RabbitAdvancedBus.PublishInternalAsync</c> does) instead of dispatching to a channel.
/// </summary>
public sealed class PublishPipelineFixture
{
    public PublishPipelineFixture()
    {
        var serializer = new SystemTextJsonSerializerV2();
        var typeNameSerializer = new DefaultTypeNameSerializer();
        SerializationStrategy = new DefaultMessageSerializationStrategy(
            typeNameSerializer, serializer, new DefaultCorrelationIdGenerationStrategy()
        );
        Conventions = new Conventions(typeNameSerializer);
        DeliveryModeStrategy = new MessageDeliveryModeStrategy(new ConnectionConfiguration());
        Services = new ServiceCollection().BuildServiceProvider();

        ProduceDelegate = new ProducePipelineBuilder()
            .UseProduceInterceptors()
            .Use(_ => ctx =>
            {
                var basicProperties = new BasicProperties();
                ctx.Properties.CopyTo(basicProperties);
                return default;
            })
            .Build();
    }

    public ProduceDelegate ProduceDelegate { get; }
    public IMessageSerializationStrategy SerializationStrategy { get; }
    public IConventions Conventions { get; }
    public IMessageDeliveryModeStrategy DeliveryModeStrategy { get; }
    public IServiceProvider Services { get; }

    /// <summary>
    ///     Mirrors <c>IAdvancedBus.PublishAsync(exchange, routingKey, mandatory, publisherConfirms, IMessage)</c>:
    ///     serialization strategy + produce pipeline.
    /// </summary>
    public ValueTask PublishAdvanced<T>(T message)
    {
        var serialized = SerializationStrategy.SerializeMessage(new Message<T>(message));
        var result = ProduceDelegate(new ProduceContext(
            "exchange", "routing.key", false, false, serialized.Properties, serialized.Body, Services, CancellationToken.None
        ));
        serialized.Dispose();
        return result;
    }

    /// <summary>
    ///     Mirrors <c>DefaultPubSub.PublishAsync</c> on top of <see cref="PublishAdvanced{T}" />:
    ///     conventions (attribute lookups), delivery mode strategy, publish configuration, <see cref="Message{T}" />.
    ///     The declare-once exchange cache hit is not modelled.
    /// </summary>
    public ValueTask PublishPubSub<T>(T message)
    {
        var messageType = typeof(T);
        var publishConfiguration = new PublishConfiguration(Conventions.TopicNamingConvention(messageType));
        var properties = new MessageProperties
        {
            Priority = 0,
            DeliveryMode = DeliveryModeStrategy.GetDeliveryMode(messageType),
        };
        var exchangeName = Conventions.ExchangeNamingConvention(messageType);

        var serialized = SerializationStrategy.SerializeMessage(new Message<T>(message, properties));
        var result = ProduceDelegate(new ProduceContext(
            exchangeName, publishConfiguration.Topic, false, publishConfiguration.PublisherConfirms ?? false,
            serialized.Properties, serialized.Body, Services, CancellationToken.None
        ));
        serialized.Dispose();
        return result;
    }
}
