using System;
using System.Collections.Generic;
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
    ///     Registers the default EasyNetQ components
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static void RegisterDefaultServices(this IServiceRegister serviceRegister)
    {
        Preconditions.CheckNotNull(serviceRegister, nameof(serviceRegister));

        // Note: IConnectionConfiguration gets registered when RabbitHutch.CreateBus(..) is run.
        // default service registration
        serviceRegister
            .Register<ILogger, NoopLogger>()
            .Register(typeof(ILogger<>), typeof(NoopLogger<>))
            .Register<IConnectionStringParser>(
                _ => new CompositeConnectionStringParser(new AmqpConnectionStringParser(), new ConnectionStringParser())
            )
            .Register<ISerializer>(_ => new JsonSerializer())
            .Register<IConventions, Conventions>()
            .Register<IEventBus, EventBus>()
            .Register<ITypeNameSerializer, DefaultTypeNameSerializer>()
            .Register<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>()
            .Register<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>()
            .Register<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>()
            .Register(new AdvancedBusEventHandlers())
            .Register<IProduceConsumeInterceptor, DefaultInterceptor>()
            .Register<IExchangeDeclareStrategy, DefaultExchangeDeclareStrategy>()
            .Register<IConsumerErrorStrategy, DefaultConsumerErrorStrategy>()
            .Register<IErrorMessageSerializer, DefaultErrorMessageSerializer>()
            .Register<IHandlerRunner, HandlerRunner>()
            .Register<IInternalConsumerFactory, InternalConsumerFactory>()
            .Register<IConsumerFactory, ConsumerFactory>()
            .Register(c => ConnectionFactoryFactory.CreateConnectionFactory(c.Resolve<ConnectionConfiguration>()))
            .Register<IChannelDispatcher, SingleChannelDispatcher>()
            .Register<IProducerConnection, ProducerConnection>()
            .Register<IConsumerConnection, ConsumerConnection>()
            .Register<IPersistentChannelFactory, PersistentChannelFactory>()
            .Register<IPublishConfirmationListener, PublishConfirmationListener>()
            .Register<IHandlerCollectionFactory, HandlerCollectionFactory>()
            .Register<IPullingConsumerFactory, PullingConsumerFactory>()
            .Register<IAdvancedBus, RabbitAdvancedBus>()
            .Register<IPubSub, DefaultPubSub>()
            .Register<IRpc, DefaultRpc>()
            .Register<ISendReceive, DefaultSendReceive>()
            .Register<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>()
            .Register<IConsumeScopeProvider, NoopConsumeScopeProvider>()
            .Register<IBus, RabbitBus>();
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
    ///     Enables interception of messages
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    /// <param name="configure">The action to add interceptors</param>
    public static IServiceRegister EnableInterception(
        this IServiceRegister serviceRegister,
        Func<IServiceResolver, IReadOnlyList<IProduceConsumeInterceptor>> configure
    )
    {
        serviceRegister.Register<IProduceConsumeInterceptor>(s => new CompositeInterceptor(configure(s)));
        return serviceRegister;
    }

    /// <summary>
    ///     Enables console logger
    /// </summary>
    /// <param name="serviceRegister">The register</param>
    public static IServiceRegister EnableConsoleLogger(this IServiceRegister serviceRegister)
    {
        serviceRegister.Register(typeof(ILogger), typeof(ConsoleLogger));
        serviceRegister.Register(typeof(ILogger<>), typeof(ConsoleLogger<>));
        return serviceRegister;
    }
}
