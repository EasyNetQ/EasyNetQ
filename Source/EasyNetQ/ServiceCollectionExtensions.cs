using EasyNetQ.Pipeline;
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
    /// <summary>
    ///     Registers the transport + core + legacy default services. Composition of
    ///     <see cref="AddEasyNetQCoreServices" /> and <see cref="AddRabbitMqServices" />.
    /// </summary>
    public static IServiceCollection RegisterDefaultServices(
        this IServiceCollection services,
        Func<IServiceProvider, ConnectionConfiguration> connectionConfigurationFactory
    )
    {
        services.AddEasyNetQCoreServices();
        services.AddRabbitMqServices(connectionConfigurationFactory);
        return services;
    }

    /// <summary>
    ///     Registers the transport-agnostic core services (registry, serialization, pipelines, facades).
    /// </summary>
    public static IServiceCollection AddEasyNetQCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessageTypeRegistry, MessageTypeRegistry>();
        services.TryAddSingleton<IMessageSerializer>(sp =>
        {
            if (sp.GetService<ISerializer>() is { } legacySerializer)
                return new Serialization.LegacyMessageSerializerAdapter(legacySerializer);

            // Source-generated modules register JsonSerializerContexts; combining them keeps serialization
            // reflection-free (and AOT-safe) for every discovered message type
            var contexts = sp.GetServices<System.Text.Json.Serialization.JsonSerializerContext>().ToArray();
            var converters = sp.GetServices<System.Text.Json.Serialization.JsonConverter>();
            return contexts.Length == 0
                ? new Serialization.SystemTextJson.SystemTextJsonMessageSerializer(
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.General), converters)
                : new Serialization.SystemTextJson.SystemTextJsonMessageSerializer(
                    System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(contexts), converters);
        });
        services.TryAddSingleton<IConventions, Conventions>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.TryAddSingleton<ITypeNameSerializer, DefaultTypeNameSerializer>();
        services.TryAddSingleton<PipelineBuilder<PublishContext>>(_ => new PipelineBuilder<PublishContext>().UseProduceInterceptors());
        services.TryAddSingleton<PipelineBuilder<ConsumeContext>>(_ =>
            new PipelineBuilder<ConsumeContext>().UseConsumeErrorStrategy().UseConsumeInterceptors());
        services.TryAddSingleton<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>();
        services.TryAddSingleton<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>();
        services.TryAddSingleton<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>();
        services.TryAddSingleton<IHandlerCollectionFactory, HandlerCollectionFactory>();
        services.TryAddSingleton<IPubSub, DefaultPubSub>();
        services.TryAddSingleton<IRpc, DefaultRpc>();
        services.TryAddSingleton<ISendReceive, DefaultSendReceive>();
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        return services;
    }

    /// <summary>
    ///     Registers the RabbitMQ transport services and projects the connection configuration into the core
    ///     <see cref="BusOptions" />.
    /// </summary>
    public static IServiceCollection AddRabbitMqServices(
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
        services.TryAddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<ConnectionConfiguration>();
            return new BusOptions
            {
                Timeout = configuration.Timeout,
                PrefetchCount = configuration.PrefetchCount,
                PersistentMessages = configuration.PersistentMessages,
                PublisherConfirms = configuration.PublisherConfirms
            };
        });
        services.TryAddSingleton<IConnectionStringParser>(
            _ => new CompositeConnectionStringParser(new AmqpConnectionStringParser(), new ConnectionStringParser())
        );
        // The transport's MessageProperties JSON converter (AMQP header values in the Error envelope); appended
        // to every SystemTextJsonMessageSerializer the DI container builds
        services.TryAddEnumerable(ServiceDescriptor.Singleton<System.Text.Json.Serialization.JsonConverter, Serialization.SystemTextJson.MessagePropertiesConverter>());
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
        services.TryAddSingleton<IPullingConsumerFactory, PullingConsumerFactory>();
        services.TryAddSingleton<IAdvancedBus, RabbitAdvancedBus>();
        services.TryAddSingleton<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        services.TryAddSingleton<IBus, RabbitBus>();
        return services;
    }
}
