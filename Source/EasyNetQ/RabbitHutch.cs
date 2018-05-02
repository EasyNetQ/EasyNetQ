using System;
using System.Collections.Generic;
#if NETFX
using System.Configuration;
#endif
using EasyNetQ.ConnectionString;
using EasyNetQ.DI;

namespace EasyNetQ
{
    /// <summary>
    /// Static methods to create EasyNetQ core APIs.
    /// </summary>
    public static class RabbitHutch
    {
#if NETFX
        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'.
        /// </summary>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus()
        {
            return CreateBus(c => {});
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'.
        /// </summary>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(Action<IServiceRegister> registerServices)
        {
            return CreateBus(AdvancedBusEventHandlers.Default, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'.
        /// </summary>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            string rabbitConnectionString;
            var rabbitConnection = ConfigurationManager.ConnectionStrings["rabbit"];
            if (rabbitConnection == null)
            {
                throw new EasyNetQException(
                    "Could not find a connection string for RabbitMQ. " +
                    "Please add a connection string in the <ConnectionStrings> section" +
                    "of the application's configuration file. For example: " +
                    "<add name=\"rabbit\" connectionString=\"host=localhost\" />");
            }
            rabbitConnectionString = rabbitConnection.ConnectionString;

            return CreateBus(rabbitConnectionString, advancedBusEventHandlers, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'.
        /// </summary>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(AdvancedBusEventHandlers advancedBusEventHandlers)
        {
            return CreateBus(advancedBusEventHandlers, c => {});
        }
#endif

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(string connectionString)
        {
            return CreateBus(connectionString, AdvancedBusEventHandlers.Default);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(string connectionString, AdvancedBusEventHandlers advancedBusEventHandlers)
        {
            return CreateBus(connectionString, advancedBusEventHandlers, x => { });
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(string connectionString, Action<IServiceRegister> registerServices)
        {
            return CreateBus(connectionString, AdvancedBusEventHandlers.Default, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(string connectionString, AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            var connectionStringParser = new ConnectionStringParser();
            var connectionConfiguration = connectionStringParser.Parse(connectionString);
            return CreateBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
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
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
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

            return CreateBus(hostName, hostPort, virtualHost, username, password, requestedHeartbeat, AdvancedBusEventHandlers.Default, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
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
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(
            string hostName,
            ushort hostPort,
            string virtualHost,
            string username,
            string password,
            ushort requestedHeartbeat,
            AdvancedBusEventHandlers advancedBusEventHandlers,
            Action<IServiceRegister> registerServices)
        {
            Preconditions.CheckNotNull(hostName, "hostName");
            Preconditions.CheckNotNull(virtualHost, "virtualHost");
            Preconditions.CheckNotNull(username, "username");
            Preconditions.CheckNotNull(password, "password");
            Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");

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
            return CreateBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionConfiguration">
        /// An <see cref="ConnectionConfiguration"/> instance.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(ConnectionConfiguration connectionConfiguration, Action<IServiceRegister> registerServices)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(registerServices, "registerServices");

            return CreateBus(connectionConfiguration, AdvancedBusEventHandlers.Default, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RabbitBus"/>.
        /// </summary>
        /// <param name="connectionConfiguration">
        /// An <see cref="ConnectionConfiguration"/> instance.
        /// </param>
        /// <param name="advancedBusEventHandlers">
        /// An <see cref="AdvancedBusEventHandlers"/> instance which is used to add handlers
        /// to the events of the newly created <see cref="IBus.Advanced"/>.
        /// As <see cref="RabbitAdvancedBus"/> attempts to connect during instantiation, specifying a <see cref="AdvancedBusEventHandlers"/>
        /// before instantiation is the only way to catch the first <see cref="AdvancedBusEventHandlers.Connected"/> event.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="RabbitBus"/> instance.
        /// </returns>
        public static IBus CreateBus(ConnectionConfiguration connectionConfiguration, AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            var container = new DefaultServiceContainer();
            container.RegisterBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
            return container.Resolve<IBus>();
        }

        public static void RegisterBus(this IServiceRegister serviceRegister, ConnectionConfiguration connectionConfiguration, AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            Preconditions.CheckNotNull(serviceRegister, "serviceRegister");
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(advancedBusEventHandlers, "advancedBusEventHandlers");
            Preconditions.CheckNotNull(registerServices, "registerServices");
            
            connectionConfiguration.Validate();
            serviceRegister.Register(connectionConfiguration);
            serviceRegister.Register(advancedBusEventHandlers);
            serviceRegister.RegisterDefaultServices();
            registerServices(serviceRegister);
        }
    }
}