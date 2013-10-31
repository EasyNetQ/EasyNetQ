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
            registerServices(serviceProvider);

            // Note: IConnectionConfiguration gets registered when RabbitHutch.CreateBus(..) is run.

            // default service registration
            serviceProvider
                .Register<IEasyNetQLogger>(x => new ConsoleLogger())
                .Register<ISerializer>(x => new JsonSerializer())
                .Register<IConventions>(x => new Conventions())
                .Register<IEventBus>(x => new EventBus())
                .Register<SerializeType>(x => TypeNameSerializer.Serialize)
                .Register<Func<string>>(x => CorrelationIdGenerator.GetCorrelationId)
                .Register<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>(x => new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>())
                .Register<IConsumerDispatcherFactory>(x => new ConsumerDispatcherFactory(x.Resolve<IEasyNetQLogger>()))
                .Register<IPublishExchangeDeclareStrategy>(x => new PublishExchangeDeclareStrategy())
                .Register<IConsumerErrorStrategy>(x => new DefaultConsumerErrorStrategy(
                    x.Resolve<IConnectionFactory>(),
                    x.Resolve<ISerializer>(),
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<IConventions>()))
                .Register<IHandlerRunner>(x => new HandlerRunner(
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<IConsumerErrorStrategy>()))
                .Register<IInternalConsumerFactory>(x => new InternalConsumerFactory(
                    x.Resolve<IHandlerRunner>(),
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<IConventions>(),
                    x.Resolve<IConnectionConfiguration>(),
                    x.Resolve<IConsumerDispatcherFactory>()))
                .Register<IConsumerFactory>(x => new ConsumerFactory(
                    x.Resolve<IInternalConsumerFactory>(),
                    x.Resolve<IEventBus>()))
                .Register<IConnectionFactory>(x => new ConnectionFactoryWrapper(
                    x.Resolve<IConnectionConfiguration>(),
                    x.Resolve<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>()))
                .Register<IMessageValidationStrategy>(x => new DefaultMessageValidationStrategy(
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<SerializeType>()))
                .Register<IPersistentChannelFactory>(x => new PersistentChannelFactory(
                    x.Resolve<IEasyNetQLogger>(), 
                    x.Resolve<IConnectionConfiguration>(),
                    x.Resolve<IEventBus>()))
                .Register<IClientCommandDispatcherFactory>(x => new ClientCommandDispatcherFactory(
                    x.Resolve<IPersistentChannelFactory>()))
                .Register<IPublisherConfirms>(x => new PublisherConfirms(
                    x.Resolve<IConnectionConfiguration>(),
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<IEventBus>()))
                .Register<IAdvancedBus>(x => new RabbitAdvancedBus(
                    x.Resolve<IConnectionFactory>(),
                    x.Resolve<SerializeType>(),
                    x.Resolve<ISerializer>(),
                    x.Resolve<IConsumerFactory>(),
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<Func<string>>(),
                    x.Resolve<IMessageValidationStrategy>(),
                    x.Resolve<IClientCommandDispatcherFactory>(),
                    x.Resolve<IPublisherConfirms>(),
                    x.Resolve<IEventBus>()))
                .Register<IRpc>(x => new Rpc(
                    x.Resolve<IAdvancedBus>(),
                    x.Resolve<IEventBus>(),
                    x.Resolve<IConventions>(),
                    x.Resolve<IPublishExchangeDeclareStrategy>(),
                    x.Resolve<IConnectionConfiguration>()))
                .Register<IBus>(x => new RabbitBus(
                    x.Resolve<SerializeType>(),
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<IConventions>(),
                    x.Resolve<IAdvancedBus>(),
                    x.Resolve<IPublishExchangeDeclareStrategy>(),
                    x.Resolve<IRpc>()
                ));

            return serviceProvider;
        }
         
    }
}