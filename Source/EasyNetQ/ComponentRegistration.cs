using System;
using EasyNetQ.Consumer;
using EasyNetQ.Loggers;
using EasyNetQ.Producer;

namespace EasyNetQ
{
    /// <summary>
    /// Registers the default EasyNetQ components in our internal super-simple IoC container.
    /// </summary>
    public class ComponentRegistration
    {
        public static IServiceProvider CreateServiceProvider(Action<IServiceRegister> registerServices)
        {
            Preconditions.CheckNotNull(registerServices, "registerServices");

            var serviceProvider = new DefaultServiceProvider();

            // gives the user a chance to register alternative service implementations.
            registerServices(serviceProvider);

            // Note: IConnectionConfiguration gets registered when RabbitHutch.CreateBus(..) is run.

            // default service registration
            serviceProvider
                .Register<IEasyNetQLogger, ConsoleLogger>()
                .Register<ISerializer, JsonSerializer>()
                .Register<IConventions, Conventions>()
                .Register<IEventBus, EventBus>()
                .Register<ITypeNameSerializer, TypeNameSerializer>()
                .Register<Func<string>>(x => CorrelationIdGenerator.GetCorrelationId)
                .Register<IClusterHostSelectionStrategy<ConnectionFactoryInfo>, DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>>()
                .Register<IConsumerDispatcherFactory, ConsumerDispatcherFactory>()
                .Register<IPublishExchangeDeclareStrategy, PublishExchangeDeclareStrategy>()
                .Register<IPublisherConfirms, PublisherConfirms>()
                .Register<IConsumerErrorStrategy, DefaultConsumerErrorStrategy>()
                .Register<IHandlerRunner, HandlerRunner>()
                .Register<IInternalConsumerFactory, InternalConsumerFactory>()
                .Register<IConsumerFactory, ConsumerFactory>()
                .Register<IConnectionFactory, ConnectionFactoryWrapper>()
                .Register<IPersistentChannelFactory, PersistentChannelFactory>()
                .Register<IClientCommandDispatcherFactory, ClientCommandDispatcherFactory>()
                .Register<IHandlerCollectionFactory, HandlerCollectionFactory>()
                .Register<IAdvancedBus, RabbitAdvancedBus>()
                .Register<IRpc, Rpc>()
                .Register<IBus, RabbitBus>();

            return serviceProvider;
        }
         
    }
}