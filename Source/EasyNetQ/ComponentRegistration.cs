using System;
using EasyNetQ.Consumer;
using EasyNetQ.Loggers;

namespace EasyNetQ
{
    public class ComponentRegistration
    {
        public static IServiceProvider CreateServiceProvider(Action<IServiceRegister> registerServices)
        {
            Preconditions.CheckNotNull(registerServices, "registerServices");

            var serviceProvider = new DefaultServiceProvider();
            registerServices(serviceProvider);

            // we only want single instances of these shared services, so instantiate them here
            var logger = new ConsoleLogger();
            var serializer = new JsonSerializer();
            var conventions = new Conventions();

            // default service registration
            serviceProvider
                .Register<IEasyNetQLogger>(x => logger)
                .Register<ISerializer>(x => serializer)
                .Register<IConventions>(x => conventions)
                .Register<SerializeType>(x => TypeNameSerializer.Serialize)
                .Register<Func<string>>(x => CorrelationIdGenerator.GetCorrelationId)
                .Register<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>(x => new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>())
                .Register<IConsumerDispatcherFactory>(x => new ConsumerDispatcherFactory(x.Resolve<IEasyNetQLogger>()))
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
                .Register<IConsumerFactory>(x => new ConsumerFactory(x.Resolve<IInternalConsumerFactory>()))
                .Register<IConnectionFactory>(x => new ConnectionFactoryWrapper(
                    x.Resolve<IConnectionConfiguration>(),
                    x.Resolve<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>()))
                .Register<IMessageValidationStrategy>(x => new DefaultMessageValidationStrategy(
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<SerializeType>()))
                .Register<IAdvancedBus>(x => new RabbitAdvancedBus(
                    x.Resolve<IConnectionFactory>(),
                    x.Resolve<SerializeType>(),
                    x.Resolve<ISerializer>(),
                    x.Resolve<IConsumerFactory>(),
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<Func<string>>(),
                    x.Resolve<IMessageValidationStrategy>()))
                .Register<IBus>(x => new RabbitBus(
                    x.Resolve<SerializeType>(),
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<IConventions>(),
                    x.Resolve<IAdvancedBus>()
                ));

            return serviceProvider;
        }
         
    }
}