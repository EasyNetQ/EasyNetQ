using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.Interception;
using EasyNetQ.Producer;
using EasyNetQ.Scheduling;

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
                .Register<ISerializer, JsonSerializer>()
                .Register<IConventions, Conventions>()
                .Register<IEventBus, EventBus>()
                .Register<ITypeNameSerializer, DefaultTypeNameSerializer>()
                .Register<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>()                
                .Register<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>()
                .Register<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>()
                .Register<ITimeoutStrategy, TimeoutStrategy>()
                .Register<IClusterHostSelectionStrategy<ConnectionFactoryInfo>, RandomClusterHostSelectionStrategy<ConnectionFactoryInfo>>()
                .Register(AdvancedBusEventHandlers.Default)
                .Register<IProduceConsumeInterceptor, DefaultInterceptor>()
                .Register<IConsumerDispatcherFactory, ConsumerDispatcherFactory>()
                .Register<IPublishExchangeDeclareStrategy, PublishExchangeDeclareStrategy>()
                .Register<IConsumerErrorStrategy, DefaultConsumerErrorStrategy>()
                .Register<IErrorMessageSerializer, DefaultErrorMessageSerializer>()
                .Register<IHandlerRunner, HandlerRunner>()
                .Register<IInternalConsumerFactory, InternalConsumerFactory>()
                .Register<IConsumerFactory, ConsumerFactory>()
                .Register<IConnectionFactory, ConnectionFactoryWrapper>()
                .Register<IPersistentChannelFactory, PersistentChannelFactory>()
                .Register<IPersistentConnectionFactory, PersistentConnectionFactory>()
                .Register<IClientCommandDispatcherFactory, ClientCommandDispatcherFactory>()
                .Register<IPublishConfirmationListener, PublishConfirmationListener>()
                .Register<IHandlerCollectionFactory, HandlerCollectionFactory>()
                .Register<IAdvancedBus, RabbitAdvancedBus>()
                .Register<IRpc, Rpc>()
                .Register<ISendReceive, SendReceive>()
                .Register<IScheduler, ExternalScheduler>()
                .Register<IBus, RabbitBus>();
        }
    }
}