using EasyNetQ.Pipeline;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Benchmarks.Fixtures;

/// <summary>
///     Builds the publish path as it runs inside the process, stopping at the transport boundary:
///     the pipeline terminal maps <see cref="MessageProperties" /> onto a fresh <see cref="BasicProperties" />
///     (as <c>RabbitAdvancedBus.PublishInternalAsync</c> does) instead of dispatching to a channel.
/// </summary>
public sealed class PublishPipelineFixture
{
    private readonly PipelineStep<PublishContext> pipeline;
    private readonly ContextPool<PublishContext> pool;

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

        pipeline = new PipelineBuilder<PublishContext>()
            .UseProduceInterceptors()
            .Build(Services, static context =>
            {
                var basicProperties = new BasicProperties();
                context.Properties.CopyTo(basicProperties);
                return default;
            });

        var channel = new ChannelContext(new ConnectionContext("Producer", Services));
        pool = new ContextPool<PublishContext>(() => new PublishContext(channel));
    }

    public IMessageSerializationStrategy SerializationStrategy { get; }
    public IConventions Conventions { get; }
    public IMessageDeliveryModeStrategy DeliveryModeStrategy { get; }
    public IServiceProvider Services { get; }

    /// <summary>
    ///     Mirrors <c>IAdvancedBus.PublishAsync(exchange, routingKey, mandatory, publisherConfirms, IMessage)</c>:
    ///     serialization strategy + pooled context + publish pipeline.
    /// </summary>
    public ValueTask PublishAdvanced<T>(T message)
    {
        var serialized = SerializationStrategy.SerializeMessage(new Message<T>(message));
        var result = Publish("exchange", "routing.key", false, serialized.Properties, serialized.Body);
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
        var result = Publish(exchangeName, publishConfiguration.Topic, publishConfiguration.PublisherConfirms ?? false, serialized.Properties, serialized.Body);
        serialized.Dispose();
        return result;
    }

    private ValueTask Publish(string exchange, string routingKey, bool publisherConfirms, in MessageProperties properties, ReadOnlyMemory<byte> body)
    {
        var context = pool.Rent();
        context.Exchange = exchange;
        context.RoutingKey = routingKey;
        context.PublisherConfirms = publisherConfirms;
        context.Properties = properties;
        context.Body = body;

        var task = pipeline(context);
        if (task.IsCompletedSuccessfully)
        {
            pool.Return(context);
            return default;
        }

        return AwaitAndReturn(task, context);
    }

    private async ValueTask AwaitAndReturn(ValueTask task, PublishContext context)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        finally
        {
            pool.Return(context);
        }
    }
}
