using EasyNetQ.ConnectionString;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ
{
    /// <summary>
    /// Static methods to create EasyNetQ core APIs.
    /// </summary>
    //[Obsolete("Use Dependency Injection to create instances of IBus.")]
    public static class RabbitHutch
    {
        /// <summary>
        /// Creates a new instance of <see cref="SelfHostedBus"/>.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
        ///
        /// The following default values will be used if not specified:
        /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
        /// </param>
        /// <returns>
        /// A new <see cref="SelfHostedBus"/> instance.
        /// </returns>
        public static SelfHostedBus CreateBus(string connectionString)
        {
            return CreateBus(connectionString, _ => { });
        }

        /// <summary>
        /// Creates a new instance of <see cref="SelfHostedBus"/>.
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
        /// A new <see cref="SelfHostedBus"/> instance.
        /// </returns>
        public static SelfHostedBus CreateBus(string connectionString, Action<IServiceCollection> registerServices)
        {
            return CreateBus(x => x.GetRequiredService<IConnectionStringParser>().Parse(connectionString), registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="SelfHostedBus"/>.
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
        /// The initially requested heartbeat interval.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="SelfHostedBus"/> instance.
        /// </returns>
        public static SelfHostedBus CreateBus(
            string hostName,
            ushort hostPort,
            string virtualHost,
            string username,
            string password,
            TimeSpan requestedHeartbeat,
            Action<IServiceCollection> registerServices)
        {
            var connectionConfiguration = new ConnectionConfiguration
            {
                Hosts = new List<HostConfiguration>(),
                Port = hostPort,
                VirtualHost = virtualHost,
                UserName = username,
                Password = password,
                RequestedHeartbeat = requestedHeartbeat
            };
            connectionConfiguration.Hosts.Add(new HostConfiguration(hostName, hostPort));
            return CreateBus(connectionConfiguration, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="SelfHostedBus"/>.
        /// </summary>
        /// <param name="connectionConfiguration">
        /// An <see cref="ConnectionConfiguration"/> instance.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="SelfHostedBus"/> instance.
        /// </returns>
        public static SelfHostedBus CreateBus(ConnectionConfiguration connectionConfiguration, Action<IServiceCollection> registerServices)
        {
            return CreateBus(_ => connectionConfiguration, registerServices);
        }

        /// <summary>
        /// Creates a new instance of <see cref="SelfHostedBus"/>.
        /// </summary>
        /// <param name="connectionConfigurationFactory">
        /// A factory of <see cref="ConnectionConfiguration"/> instance.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        /// <returns>
        /// A new <see cref="SelfHostedBus"/> instance.
        /// </returns>
        public static SelfHostedBus CreateBus(Func<IServiceProvider, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceCollection> registerServices)
        {
            var services = new ServiceCollection();
            RegisterBus(services, connectionConfigurationFactory, registerServices);
            var serviceProvider = services.BuildServiceProvider();
            return new SelfHostedBus(serviceProvider);
        }

        /// <summary>
        /// Registers components of a <see cref="SelfHostedBus"/>.
        /// </summary>
        /// <param name="services"/>
        /// <param name="connectionConfigurationFactory">
        /// A factory of <see cref="ConnectionConfiguration"/> instance.
        /// </param>
        /// <param name="registerServices">
        /// Override default services. For example, to override the default <see cref="ISerializer"/>:
        /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
        /// </param>
        public static void RegisterBus(
            IServiceCollection services,
            Func<IServiceProvider, ConnectionConfiguration> connectionConfigurationFactory,
            Action<IServiceCollection> registerServices)
        {
            // First call delegate to register user-supplied services
            registerServices(services);

            // Then register default services
            services.RegisterDefaultServices(connectionConfigurationFactory);
            services.AddSingleton<IConnectionStringParser, ConnectionStringParser>();
            services.AddSingleton(connectionConfigurationFactory);
        }
    }
}
