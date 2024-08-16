using EasyNetQ.ChannelDispatcher;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace EasyNetQ;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterDefaultServices(
        this IServiceCollection services,
        Func<IServiceProvider, ConnectionConfiguration> connectionConfigurationFactory
    )
    {
        services.TryAddSingleton(s =>
        {
            var configuration = connectionConfigurationFactory(s);
            configuration.SetDefaultProperties();
            return configuration;
        });
        services.TryAddSingleton<IConnectionStringParser>(
            _ => new CompositeConnectionStringParser(new AmqpConnectionStringParser(), new ConnectionStringParser())
        );
        services.TryAddSingleton<ISerializer>(_ => new ReflectionBasedNewtonsoftJsonSerializer());
        services.TryAddSingleton<IConventions, Conventions>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.TryAddSingleton<ITypeNameSerializer, DefaultTypeNameSerializer>();
        services.TryAddSingleton<ProducePipelineBuilder>(_ => new ProducePipelineBuilder().UseProduceInterceptors());
        services.TryAddSingleton<ConsumePipelineBuilder>(_ =>
            new ConsumePipelineBuilder().UseConsumeErrorStrategy().UseConsumeInterceptors());
        services.TryAddSingleton<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>();
        services.TryAddSingleton<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>();
        services.TryAddSingleton<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>();
        services.TryAddSingleton<AdvancedBusEventHandlers>(_ => new AdvancedBusEventHandlers());
        services.TryAddSingleton<IExchangeDeclareStrategy, DefaultExchangeDeclareStrategy>();
        services.TryAddSingleton<IConsumeErrorStrategy, DefaultConsumeErrorStrategy>();
        services.TryAddSingleton<IErrorMessageSerializer, DefaultErrorMessageSerializer>();
        services.TryAddSingleton<IInternalConsumerFactory, InternalConsumerFactory>();
        services.TryAddSingleton<IConsumerFactory, ConsumerFactory>();
        services.TryAddSingleton<IConnectionFactory>(serviceProvider =>
        {
            var connectionConfiguration = serviceProvider.GetRequiredService<ConnectionConfiguration>();
            return ConnectionFactoryFactory.CreateConnectionFactory(connectionConfiguration);
        });
        services.TryAddSingleton<IPersistentChannelDispatcher, SinglePersistentChannelDispatcher>();
        services.TryAddSingleton<IProducerConnection, ProducerConnection>();
        services.TryAddSingleton<IConsumerConnection, ConsumerConnection>();
        services.TryAddSingleton<IPersistentChannelFactory, PersistentChannelFactory>();
        services.TryAddSingleton<IPublishConfirmationListener, PublishConfirmationListener>();
        services.TryAddSingleton<IHandlerCollectionFactory, HandlerCollectionFactory>();
        services.TryAddSingleton<IPullingConsumerFactory, PullingConsumerFactory>();
        services.TryAddSingleton<IAdvancedBus, RabbitAdvancedBus>();
        services.TryAddSingleton<IPubSub, DefaultPubSub>();
        services.TryAddSingleton<IRpc, DefaultRpc>();
        services.TryAddSingleton<ISendReceive, DefaultSendReceive>();
        services.TryAddSingleton<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        services.TryAddSingleton<IBus, RabbitBus>();
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddSingleton<ILoggerFactory>(x=> new NullLoggerFactory());
        return services;
    }
}
