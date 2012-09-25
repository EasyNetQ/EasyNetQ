using System;
using EasyNetQ.Loggers;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class ComponentRegistration
    {
        public static IServiceProvider CreateServiceProvider(Action<IServiceRegister> registerServices)
        {
            var serviceProvider = new DefaultServiceProvider();

            // default service registration
            serviceProvider
                .Register<IEasyNetQLogger>(x => new ConsoleLogger())
                .Register<ISerializer>(x => new JsonSerializer())
                .Register<IConventions>(x => new Conventions())
                .Register<SerializeType>(x => TypeNameSerializer.Serialize)
                .Register<Func<string>>(x => CorrelationIdGenerator.GetCorrelationId)
                .Register<IConsumerErrorStrategy>(x => new DefaultConsumerErrorStrategy(
                    x.Resolve<IConnectionFactory>(),
                    x.Resolve<ISerializer>(),
                    x.Resolve<IEasyNetQLogger>()))
                .Register<IConsumerFactory>(x => new QueueingConsumerFactory(
                    x.Resolve<IEasyNetQLogger>(),
                    x.Resolve<IConsumerErrorStrategy>()
                    ))
                .Register<IConnectionFactory>(x =>
                {
                    var configuration = x.Resolve<IConnectionConfiguration>();
                    var rabbitConnectionFactory = new ConnectionFactory
                    {
                        HostName = configuration.Host,
                        Port = configuration.Port,
                        VirtualHost = configuration.VirtualHost,
                        UserName = configuration.UserName,
                        Password = configuration.Password
                    };
                    return new ConnectionFactoryWrapper(rabbitConnectionFactory);
                })
                .Register<IAdvancedBus>(x => new RabbitAdvancedBus(
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

            registerServices(serviceProvider);
            return serviceProvider;
        }
         
    }
}