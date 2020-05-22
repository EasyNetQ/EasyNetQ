using System;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Interception;
using EasyNetQ.MessageVersioning;
using EasyNetQ.MultipleExchange;
using EasyNetQ.Producer;
using EasyNetQ.Scheduling;
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
        /// <param name="serviceRegister"></param>
        public static void RegisterDefaultServices(this IServiceRegister serviceRegister)
        {
            Preconditions.CheckNotNull(serviceRegister, "container");

            // Note: IConnectionConfiguration gets registered when RabbitHutch.CreateBus(..) is run.
            // default service registration
            serviceRegister
                .Register<IConnectionStringParser, ConnectionStringParser>()
                .Register<ISerializer>(_ => new JsonSerializer())
                .Register<IConventions, Conventions>()
                .Register<IEventBus, EventBus>()
                .Register<ITypeNameSerializer, DefaultTypeNameSerializer>()
                .Register<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>()
                .Register<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>()
                .Register<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>()
                .Register(AdvancedBusEventHandlers.Default)
                .Register<IProduceConsumeInterceptor, DefaultInterceptor>()
                .Register<IConsumerDispatcherFactory, ConsumerDispatcherFactory>()
                .Register<IExchangeDeclareStrategy, DefaultExchangeDeclareStrategy>()
                .Register<IConsumerErrorStrategy, DefaultConsumerErrorStrategy>()
                .Register<IErrorMessageSerializer, DefaultErrorMessageSerializer>()
                .Register<IHandlerRunner, HandlerRunner>()
                .Register<IInternalConsumerFactory, InternalConsumerFactory>()
                .Register<IConsumerFactory, ConsumerFactory>()
                .Register(c =>
                {
                    var connectionConfiguration = c.Resolve<ConnectionConfiguration>();
                    return ConnectionFactoryFactory.CreateConnectionFactory(connectionConfiguration);
                })
                .Register<IClientCommandDispatcher, SingleChannelClientCommandDispatcher>()
                .Register<IPersistentConnection>(c =>
                {
                    var connection = new PersistentConnection(c.Resolve<ConnectionConfiguration>(),
                        c.Resolve<IConnectionFactory>(), c.Resolve<IEventBus>());
                    connection.Initialize();
                    return connection;
                })
                .Register<IPersistentChannelFactory, PersistentChannelFactory>()
                .Register<IPublishConfirmationListener, PublishConfirmationListener>()
                .Register<IHandlerCollectionFactory, HandlerCollectionFactory>()
                .Register<IAdvancedBus, RabbitAdvancedBus>()
                .Register<IPubSub, DefaultPubSub>()
                .Register<IRpc, DefaultRpc>()
                .Register<ISendReceive, DefaultSendReceive>()
                .Register<IScheduler, ExternalScheduler>()
                .Register<IBus, RabbitBus>();
        }

        public static IServiceRegister EnableMultiChannelClientCommandDispatcher(
            this IServiceRegister serviceRegister, int channelsCount
        )
        {
            return serviceRegister.Register<IClientCommandDispatcher>(
                x => new MultiChannelClientCommandDispatcher(
                    channelsCount, x.Resolve<IPersistentConnection>(), x.Resolve<IPersistentChannelFactory>()
                )
            );
        }

        public static IServiceRegister EnableLegacyTypeNaming(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<ITypeNameSerializer, LegacyTypeNameSerializer>();
        }

        public static IServiceRegister EnableLegacyRpcConventions(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IConventions, LegacyRpcConventions>();
        }

        public static IServiceRegister EnableLegacyConventions(this IServiceRegister serviceRegister)
        {
            return serviceRegister
                .EnableLegacyTypeNaming()
                .EnableLegacyRpcConventions();
        }

        public static IServiceRegister EnableDelayedExchangeScheduler(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler, DelayedExchangeScheduler>();
        }

        public static IServiceRegister EnableDeadLetterExchangeAndMessageTtlScheduler(
            this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        }

        public static IServiceRegister EnableAdvancedMessagePolymorphism(this IServiceRegister serviceRegister)
        {
            return serviceRegister
                .Register<IExchangeDeclareStrategy, MultipleExchangeDeclareStrategy>();
        }

        public static IServiceRegister EnableMessageVersioning(this IServiceRegister serviceRegister)
        {
            return serviceRegister
                .Register<IExchangeDeclareStrategy, VersionedExchangeDeclareStrategy>()
                .Register<IMessageSerializationStrategy, VersionedMessageSerializationStrategy>();
        }

        public static IServiceRegister EnableInterception(
            this IServiceRegister serviceRegister,
            Action<IInterceptorRegistrator> configure
        )
        {
            var registrator = new InterceptorRegistrator(serviceRegister);
            configure(registrator);
            return registrator.Register();
        }

        public static IInterceptorRegistrator EnableGZipCompression(this IInterceptorRegistrator interceptorRegistrator)
        {
            interceptorRegistrator.Add(new GZipInterceptor());
            return interceptorRegistrator;
        }

        public static IInterceptorRegistrator EnableTripleDESEncryption(
            this IInterceptorRegistrator interceptorRegistrator, byte[] key, byte[] iv)
        {
            interceptorRegistrator.Add(new TripleDESInterceptor(key, iv));
            return interceptorRegistrator;
        }
    }
}
