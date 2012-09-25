using System;
using System.Configuration;
using EasyNetQ.Loggers;
using RabbitMQ.Client;

namespace EasyNetQ
{
    /// <summary>
    /// Does poor man's dependency injection. Supplies default instances of services required by
    /// RabbitBus.
    /// </summary>
    public static class RabbitHutch
    {
        /// <summary>
        /// Creates a new instance of RabbitBus.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=0
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string connectionString)
        {
            return CreateBus(connectionString, x => {});
        }

        /// <summary>
        /// Creates a new instance of RabbitBus.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=0
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default IEasyNetQLogger:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register&lt;IEasyNetQLogger&gt;(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string connectionString, Action<IServiceRegister> registerServices)
        {
            if(connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            var connectionValues = new ConnectionString(connectionString);

            return CreateBus(connectionValues, registerServices);
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// </summary>
        /// <param name="hostName">
        /// The RabbitMQ broker.
        /// </param>
        /// <param name="hostPort">
        /// The RabbitMQ broker port.
        /// </param>
        /// <param name="virtualHost">
        /// The RabbitMQ virtualHost.
        /// </param>
        /// <param name="username">
        /// The username to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="password">
        /// The password to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="requestedHeartbeat">
        /// The initially requested heartbeat interval, in seconds; zero for none.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default IEasyNetQLogger:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register&lt;IEasyNetQLogger&gt;(_ => myLogger));
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(
            string hostName, 
            ushort hostPort, 
            string virtualHost, 
            string username, 
            string password,
            ushort requestedHeartbeat, 
            Action<IServiceRegister> registerServices)
        {
            if(hostName == null)
            {
                throw new ArgumentNullException("hostName");
            }
            if(virtualHost == null)
            {
                throw new ArgumentNullException("virtualHost");
            }
            if(username == null)
            {
                throw new ArgumentNullException("username");
            }
            if(password == null)
            {
                throw new ArgumentNullException("password");
            }
            if(registerServices == null)
            {
                throw new ArgumentNullException("registerServices");
            }

            var connectionConfiguration = new ConnectionConfiguration
            {
                Host = hostName,
                Port = hostPort,
                VirtualHost = virtualHost,
                UserName = username,
                Password = password,
                RequestedHeartbeat = requestedHeartbeat
            };

            return CreateBus(connectionConfiguration, registerServices);
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// </summary>
        /// <param name="connectionConfiguration">
        /// An IConnectionConfiguration instance.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default IEasyNetQLogger:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register&lt;IEasyNetQLogger&gt;(_ => myLogger));
        /// </param>
        /// <returns></returns>
        public static IBus CreateBus(IConnectionConfiguration connectionConfiguration, Action<IServiceRegister> registerServices)
        {
            Action<IServiceRegister> registerServices2 = x =>
            {
                x.Register(_ => connectionConfiguration);
                registerServices(x);
            };

            var serviceProvider = CreateServiceProvider(registerServices2);

            return serviceProvider.Resolve<IBus>();
        }

        private static IServiceProvider CreateServiceProvider(Action<IServiceRegister> registerServices)
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

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'
        /// </summary>
        /// <returns></returns>
        public static IBus CreateBus()
        {
            var rabbitConnectionString = ConfigurationManager.ConnectionStrings["rabbit"];
            if (rabbitConnectionString == null)
            {
                throw new EasyNetQException(
                    "Could not find a connection string for RabbitMQ. " +
                    "Please add a connection string in the <ConnectionStrings> secion" +
                    "of the application's configuration file. For example: " +
                    "<add name=\"rabbit\" connectionString=\"host=localhost\" />");
            }

            return CreateBus(rabbitConnectionString.ConnectionString);
        }
    }
}