using System;
using EasyNetQ.Loggers;

namespace EasyNetQ
{
    public class ComponentRegistration
    {
        public static IServiceProvider CreateServiceProvider(Action<IServiceRegister> registerServices)
        {
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
                .Register<IConsumerErrorStrategy>(x => new DefaultConsumerErrorStrategy(
                    x.Resolve<IConnectionFactory>(),
                    x.Resolve<ISerializer>(),
                    x.Resolve<IEasyNetQLogger>()))
                .Register<IConsumerFactory>(x => new QueueingConsumerFactory(
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<IConsumerErrorStrategy>()))
                .Register<IConnectionFactory>(x => new ConnectionFactoryWrapper(
                    x.Resolve<IConnectionConfiguration>(),
                    x.Resolve<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>()))
                .Register<IAdvancedBus>(x => new RabbitAdvancedBus(
                    x.Resolve<IConnectionConfiguration>(),
                    x.Resolve<IConnectionFactory>(),
                    x.Resolve<SerializeType>(),
                    x.Resolve<ISerializer>(),
                    x.Resolve<IConsumerFactory>(),
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<Func<string>>(),
                    x.Resolve<IConventions>()))
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