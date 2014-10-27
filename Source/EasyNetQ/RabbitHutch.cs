using System;
using System.Collections.Generic;
using System.Configuration;
using EasyNetQ.ConnectionString;

namespace EasyNetQ
{
    /// <summary>
    /// Static methods to create EasyNetQ core APIs.
    /// </summary>
    public static class RabbitHutch
    {
        private static Func<IContainer> createContainerInternal = () => new DefaultServiceProvider();

        /// <summary>
        /// Set the container creation function. This allows you to replace EasyNetQ's default internal
        /// IoC container. Note that all components should be registered as singletons. EasyNetQ will
        /// also call Dispose on components that are no longer required.
        /// </summary>
        public static void SetContainerFactory(Func<IContainer> createContainer)
        {
            Preconditions.CheckNotNull(createContainer, "createContainer");

            createContainerInternal = createContainer;
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
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string connectionString)
        {
            Preconditions.CheckNotNull(connectionString, "connectionString");

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
            Preconditions.CheckNotNull(connectionString, "connectionString");
            Preconditions.CheckNotNull(registerServices, "registerServices");

            var connectionStringParser = new ConnectionStringParser();

            var connectionConfiguration = connectionStringParser.Parse(connectionString);

            return CreateBus(connectionConfiguration, registerServices);
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
            Preconditions.CheckNotNull(hostName, "hostName");
            Preconditions.CheckNotNull(virtualHost, "virtualHost");
            Preconditions.CheckNotNull(username, "username");
            Preconditions.CheckNotNull(password, "password");
            Preconditions.CheckNotNull(registerServices, "registerServices");

            var connectionConfiguration = new ConnectionConfiguration
            {
                Hosts = new List<HostConfiguration>
                    {
                        new HostConfiguration { Host = hostName, Port = hostPort }
                    },
                Port = hostPort,
                VirtualHost = virtualHost,
                UserName = username,
                Password = password,
                RequestedHeartbeat = requestedHeartbeat
            };
            connectionConfiguration.Validate();

            return CreateBus(connectionConfiguration, registerServices);
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// </summary>
        /// <param name="connectionConfiguration">
        /// An ConnectionConfiguration instance.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default IEasyNetQLogger:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register&lt;IEasyNetQLogger&gt;(_ => myLogger));
        /// </param>
        /// <returns></returns>
        public static IBus CreateBus(ConnectionConfiguration connectionConfiguration, Action<IServiceRegister> registerServices)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(registerServices, "registerServices");

            var container = createContainerInternal();
            if (container == null)
            {
                throw new EasyNetQException("Could not create container. " + 
                    "Have you called SetContainerFactory(...) with a function that returns null?");
            }

            registerServices(container);
            container.Register(_ => connectionConfiguration);
            ComponentRegistration.RegisterServices(container);

            return container.Resolve<IBus>();
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
                    "Please add a connection string in the <ConnectionStrings> section" +
                    "of the application's configuration file. For example: " +
                    "<add name=\"rabbit\" connectionString=\"host=localhost\" />");
            }

            return CreateBus(rabbitConnectionString.ConnectionString);
        }

    }
}