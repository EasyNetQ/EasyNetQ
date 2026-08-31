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
        // RabbitMQ registrations first: Core's fallbacks (e.g. SimpleConsumeErrorStrategy) are TryAdd and
        // must not shadow the RabbitMQ defaults (e.g. the error-queue strategy)
        services.AddRabbitMqServices(connectionConfigurationFactory);
        CoreServiceCollectionExtensions.AddEasyNetQCoreServices(services);
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
        services.TryAddSingleton<IPullingConsumerFactory, PullingConsumerFactory>();
        services.TryAddSingleton<Transport.ITransport, Transport.RabbitMqTransport>();
        services.TryAddSingleton<IAdvancedBus, RabbitAdvancedBus>();
        services.TryAddSingleton<IPubSub, DefaultPubSub>();
        services.TryAddSingleton<IRpc, DefaultRpc>();
        services.TryAddSingleton<ISendReceive, DefaultSendReceive>();
        services.TryAddSingleton<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        services.TryAddSingleton<IBus, RabbitBus>();
        return services;
    }
}
