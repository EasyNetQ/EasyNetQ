using System;
using EasyNetQ.ChannelDispatcher;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Interception;
using EasyNetQ.Logging;
using EasyNetQ.MessageVersioning;
using EasyNetQ.MultipleExchange;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;

namespace EasyNetQ;

/// <summary>
///     Registers the EasyNetQ components
/// </summary>
public static class ServiceRegisterExtensions
{
    /// <summary>
    /// Registers the default EasyNetQ components if needed services have not yet been registered.
    /// </summary>
    public static void RegisterDefaultServices(
        this IServiceRegister serviceRegister,
        Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory
    )
    {
        Preconditions.CheckNotNull(serviceRegister, nameof(serviceRegister));

        serviceRegister
            .TryRegister(resolver =>
            {
                var configuration = connectionConfigurationFactory(resolver);
                configuration.SetDefaultProperties();
                return configuration;
            })
            .TryRegister<ILogger, NoopLogger>()
            .TryRegister(typeof(ILogger<>), typeof(NoopLogger<>))
            .TryRegister<IConnectionStringParser>(
                _ => new CompositeConnectionStringParser(new AmqpConnectionStringParser(), new ConnectionStringParser())
            )
            .TryRegister<ISerializer>(_ => new JsonSerializer())
            .TryRegister<IConventions, Conventions>()
            .TryRegister<IEventBus, EventBus>()
            .TryRegister<ITypeNameSerializer, DefaultTypeNameSerializer>()
            .TryRegister<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>()
            .TryRegister<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>()
            .TryRegister<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>()
            .TryRegister(new AdvancedBusEventHandlers())
            .TryRegister<IExchangeDeclareStrategy, DefaultExchangeDeclareStrategy>()
            .TryRegister<IConsumerErrorStrategy, DefaultConsumerErrorStrategy>()
            .TryRegister<IErrorMessageSerializer, DefaultErrorMessageSerializer>()
            .TryRegister<IHandlerRunner, HandlerRunner>()
            .TryRegister<IInternalConsumerFactory, InternalConsumerFactory>()
            .TryRegister<IConsumerFactory, ConsumerFactory>()
            .TryRegister(c => ConnectionFactoryFactory.CreateConnectionFactory(c.Resolve<ConnectionConfiguration>()))
            .TryRegister<IChannelDispatcher, SingleChannelDispatcher>()
            .TryRegister<IProducerConnection, ProducerConnection>()
            .TryRegister<IConsumerConnection, ConsumerConnection>()
            .TryRegister<IPersistentChannelFactory, PersistentChannelFactory>()
            .TryRegister<IPublishConfirmationListener, PublishConfirmationListener>()
            .TryRegister<IHandlerCollectionFactory, HandlerCollectionFactory>()
            .TryRegister<IPullingConsumerFactory, PullingConsumerFactory>()
            .TryRegister<IAdvancedBus, RabbitAdvancedBus>()
            .TryRegister<IPubSub, DefaultPubSub>()
            .TryRegister<IRpc, DefaultRpc>()
            .TryRegister<ISendReceive, DefaultSendReceive>()
            .TryRegister<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>()
            .TryRegister<IConsumeScopeProvider, NoopConsumeScopeProvider>()
            .TryRegister<IProduceConsumeInterceptor, NoopProduceConsumeInterceptor>()
            .TryRegister<IBus, RabbitBus>();
    }

    /// <summary>
    ///     Enables support of using multiple channels for clients operations
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    /// <param name="channelsCount">Max count of channels</param>
    public static IServiceRegister EnableMultiChannelClientCommandDispatcher(
        this IServiceRegister serviceRegister, int channelsCount
    )
    {
        return serviceRegister.Register<IChannelDispatcher>(
            x => new MultiChannelDispatcher(
                channelsCount,
                x.Resolve<IProducerConnection>(),
                x.Resolve<IConsumerConnection>(),
                x.Resolve<IPersistentChannelFactory>()
            )
        );
    }

    /// <summary>
    ///     Enables legacy type naming. See <see cref="LegacyTypeNameSerializer"/> for more details
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableLegacyTypeNaming(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register<ITypeNameSerializer, LegacyTypeNameSerializer>();
    }

    /// <summary>
    ///     Enables legacy rpc conventions. See <see cref="LegacyRpcConventions"/> for more details
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableLegacyRpcConventions(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register<IConventions, LegacyRpcConventions>();
    }

    /// <summary>
    ///     Enables all legacy conventions
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableLegacyConventions(this IServiceRegister serviceRegister)
    {
        return serviceRegister
            .EnableLegacyTypeNaming()
            .EnableLegacyRpcConventions();
    }

    /// <summary>
    ///     Enables support of scheduling messages using delayed exchange plugin.
    ///     See <see cref="DelayedExchangeScheduler"/> for more details
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableDelayedExchangeScheduler(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register<IScheduler, DelayedExchangeScheduler>();
    }

    /// <summary>
    ///     Enables AdvancedMessagePolymorphism. See <see cref="MultipleExchangeDeclareStrategy"/> for more details
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableAdvancedMessagePolymorphism(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register<IExchangeDeclareStrategy, MultipleExchangeDeclareStrategy>();
    }

    /// <summary>
    ///     Enables versioning of messages.
    ///     See <see cref="VersionedExchangeDeclareStrategy"/> and
    ///     <see cref="VersionedMessageSerializationStrategy"/> for more details
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableMessageVersioning(this IServiceRegister serviceRegister)
    {
        return serviceRegister
            .Register<IExchangeDeclareStrategy, VersionedExchangeDeclareStrategy>()
            .Register<IMessageSerializationStrategy, VersionedMessageSerializationStrategy>();
    }

    /// <summary>
    ///     Enables console logger
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableConsoleLogger(this IServiceRegister serviceRegister)
    {
        return serviceRegister
            .Register(typeof(ILogger), typeof(ConsoleLogger))
            .Register(typeof(ILogger<>), typeof(ConsoleLogger<>));
    }

    /// <summary>
    ///     Enables a consumer error strategy which acks failed messages
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableAlwaysAckConsumerErrorStrategy(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register<IConsumerErrorStrategy>(SimpleConsumerErrorStrategy.Ack);
    }

    /// <summary>
    ///     Enables a consumer error strategy which nacks failed messages with requeue
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableAlwaysNackWithRequeueConsumerErrorStrategy(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register<IConsumerErrorStrategy>(SimpleConsumerErrorStrategy.NackWithRequeue);
    }

    /// <summary>
    ///     Enables a consumer error strategy which nacks failed messages without requeue
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableAlwaysNackWithoutRequeueConsumerErrorStrategy(this IServiceRegister serviceRegister)
    {
        return serviceRegister.Register<IConsumerErrorStrategy>(SimpleConsumerErrorStrategy.NackWithoutRequeue);
    }
}
