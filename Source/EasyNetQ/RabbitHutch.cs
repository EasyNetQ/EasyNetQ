using EasyNetQ.ConnectionString;
using EasyNetQ.DI;
using EasyNetQ.LightInject;

namespace EasyNetQ;

/// <summary>
/// Static methods to create EasyNetQ core APIs.
/// </summary>
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
    /// A new <see cref="RabbitBus"/> instance.
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
    /// A new <see cref="RabbitBus"/> instance.
    /// </returns>
    public static SelfHostedBus CreateBus(string connectionString, Action<IServiceRegister> registerServices)
    {
        return CreateBus(x => x.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
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
    /// The initially requested heartbeat interval
    /// </param>
    /// <param name="registerServices">
    /// Override default services. For example, to override the default <see cref="ISerializer"/>:
    /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
    /// </param>
    /// <returns>
    /// A new <see cref="RabbitBus"/> instance.
    /// </returns>
    public static SelfHostedBus CreateBus(
        string hostName,
        ushort hostPort,
        string virtualHost,
        string username,
        string password,
        TimeSpan requestedHeartbeat,
        Action<IServiceRegister> registerServices
    )
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
    /// A new <see cref="RabbitBus"/> instance.
    /// </returns>
    public static SelfHostedBus CreateBus(ConnectionConfiguration connectionConfiguration, Action<IServiceRegister> registerServices)
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
    /// A new <see cref="RabbitBus"/> instance.
    /// </returns>
    public static SelfHostedBus CreateBus(Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
    {
        var container = new ServiceContainer(c => c.EnablePropertyInjection = false);
        var adapter = new LightInjectAdapter(container);
        RegisterBus(adapter, connectionConfigurationFactory, registerServices);
        return new SelfHostedBus(container);
    }

    /// <summary>
    /// Registers components of a <see cref="RabbitBus"/>.
    /// </summary>
    /// <param name="serviceRegister"/>
    /// <param name="connectionConfigurationFactory">
    /// A factory of <see cref="ConnectionConfiguration"/> instance.
    /// </param>
    /// <param name="registerServices">
    /// Override default services. For example, to override the default <see cref="ISerializer"/>:
    /// RabbitHutch.CreateBus("host=localhost", x => x.Register{ISerializer}(mySerializer));
    /// </param>
    public static void RegisterBus(
        IServiceRegister serviceRegister,
        Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory,
        Action<IServiceRegister> registerServices
    )
    {
        // first call delegate to register user-supplied services
        registerServices(serviceRegister);

        // then register default services
        serviceRegister.RegisterDefaultServices(connectionConfigurationFactory);
    }
}
