using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.Interception;
using EasyNetQ.Producer;
using EasyNetQ.Scheduling;
using RabbitMQ.Client;

namespace EasyNetQ.DI
{
    /// <summary>
    /// Registers the default EasyNetQ components
    /// </summary>
    public static class DefaultServicesRegistration
    {
        public static void RegisterDefaultServices(this IServiceRegister container)
        {
            Preconditions.CheckNotNull(container, "container");

            // Note: IConnectionConfiguration gets registered when RabbitHutch.CreateBus(..) is run.
            // default service registration
            container
                .Register<IConnectionStringParser, ConnectionStringParser>()
                .Register<ISerializer>(_ => new JsonSerializer())
                .Register<IConventions, Conventions>()
                .Register<IEventBus, EventBus>()
                .Register<ITypeNameSerializer, DefaultTypeNameSerializer>()
                .Register<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>()
                .Register<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>()
                .Register<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>()
                .Register<ITimeoutStrategy, TimeoutStrategy>()
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
                .Register<IClientCommandDispatcher, ClientCommandDispatcher>()
                .Register<IPersistentConnection>(c =>
                {
                    var connection = new PersistentConnection(c.Resolve<ConnectionConfiguration>(), c.Resolve<IConnectionFactory>(), c.Resolve<IEventBus>());
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
    }
}
