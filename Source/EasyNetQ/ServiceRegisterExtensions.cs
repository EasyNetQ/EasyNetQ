using System;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Interception;
using EasyNetQ.MessageVersioning;
using EasyNetQ.MultipleExchange;
using EasyNetQ.Producer;
using RabbitMQ.Client;

namespace EasyNetQ
{
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
            Preconditions.CheckNotNull(serviceRegister, "container");

            // Note: IConnectionConfiguration gets registered when RabbitHutch.CreateBus(..) is run.
            // default service registration
            serviceRegister
                .Register<IConnectionStringParser>(
                    x => new CompositeConnectionStringParser(new AmqpConnectionStringParser(), new ConnectionStringParser())
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
                .Register<IConsumerDispatcherFactory, ConsumerDispatcherFactory>()
                .Register<IExchangeDeclareStrategy, DefaultExchangeDeclareStrategy>()
                .Register<IConsumerErrorStrategy, DefaultConsumerErrorStrategy>()
                .Register<IErrorMessageSerializer, DefaultErrorMessageSerializer>()
                .Register<IHandlerRunner, HandlerRunner>()
                .Register<IInternalConsumerFactory, InternalConsumerFactory>()
                .Register<IConsumerFactory, ConsumerFactory>()
                .Register(c => ConnectionFactoryFactory.CreateConnectionFactory(c.Resolve<ConnectionConfiguration>()))
                .Register<IClientCommandDispatcher, SingleChannelClientCommandDispatcher>()
                .Register<IPersistentConnection, PersistentConnection>()
                .Register<IPersistentChannelFactory, PersistentChannelFactory>()
                .Register<IPublishConfirmationListener, PublishConfirmationListener>()
                .Register<IHandlerCollectionFactory, HandlerCollectionFactory>()
                .Register<IPullingConsumerFactory, PullingConsumerFactory>()
                .Register<IAdvancedBus, RabbitAdvancedBus>()
                .Register<IPubSub, DefaultPubSub>()
                .Register<IRpc, DefaultRpc>()
                .Register<ISendReceive, DefaultSendReceive>()
                .Register<IScheduler>(
                    x => new DeadLetterExchangeAndMessageTtlScheduler(
                        x.Resolve<ConnectionConfiguration>(),
                        x.Resolve<IAdvancedBus>(),
                        x.Resolve<IConventions>(),
                        x.Resolve<IMessageDeliveryModeStrategy>(),
                        x.Resolve<IExchangeDeclareStrategy>()
                    )
                )
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
            return serviceRegister.Register<IClientCommandDispatcher>(
                x => new MultiChannelClientCommandDispatcher(channelsCount, x.Resolve<IPersistentChannelFactory>())
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
        ///     Enables legacy support of scheduling messages using DLX + Message TTL.
        ///     See <see cref="DeadLetterExchangeAndMessageTtlScheduler"/> for more details
        /// </summary>
        /// <param name="serviceRegister">The register</param>
        public static IServiceRegister EnableLegacyDeadLetterExchangeAndMessageTtlScheduler(
            this IServiceRegister serviceRegister
        )
        {
            return serviceRegister.Register<IScheduler>(
                x => new DeadLetterExchangeAndMessageTtlScheduler(
                    x.Resolve<ConnectionConfiguration>(),
                    x.Resolve<IAdvancedBus>(),
                    x.Resolve<IConventions>(),
                    x.Resolve<IMessageDeliveryModeStrategy>(),
                    x.Resolve<IExchangeDeclareStrategy>(),
                    true
                )
            );
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
            Action<IInterceptorRegistrator> configure
        )
        {
            var registrator = new InterceptorRegistrator(serviceRegister);
            configure(registrator);
            return registrator.Register();
        }

        /// <summary>
        ///     Enables gzip compression interceptor
        /// </summary>
        /// <param name="registrator">The registrator</param>
        public static IInterceptorRegistrator EnableGZipCompression(this IInterceptorRegistrator registrator)
        {
            registrator.Add(new GZipInterceptor());
            return registrator;
        }

        /// <summary>
        ///     Enables triple DES interceptor
        /// </summary>
        /// <param name="registrator">The registrator</param>
        /// <param name="key">the secret key for the TripleDES algorithm</param>
        /// <param name="iv">The initialization vector (IV) for the symmetric algorithm</param>
        public static IInterceptorRegistrator EnableTripleDESEncryption(
            this IInterceptorRegistrator registrator, byte[] key, byte[] iv
        )
        {
            registrator.Add(new TripleDESInterceptor(key, iv));
            return registrator;
        }
    }
}
