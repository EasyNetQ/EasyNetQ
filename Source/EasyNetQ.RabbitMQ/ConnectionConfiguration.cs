using RabbitMQ.Client;

namespace EasyNetQ;

/// <summary>
///     Contains various settings of a connection and more
/// </summary>
public class ConnectionConfiguration
{
    /// <summary>
    ///     Default AMQP port
    /// </summary>
    public const int DefaultPort = 5672;

    /// <summary>
    ///     Default secured AMQP port
    /// </summary>
    public const int DefaultAmqpsPort = 5671;

    /// <summary>
    /// </summary>
    public ConnectionConfiguration()
    {
        ClientName = "EasyNetQ";
        Port = DefaultPort;
        VirtualHost = "/";
        UserName = "guest";
        Password = "guest";
        RequestedHeartbeat = TimeSpan.FromSeconds(10);
        Timeout = TimeSpan.FromSeconds(10);
        PublisherConfirms = false;
        PersistentMessages = true;
        ConnectIntervalAttempt = TimeSpan.FromSeconds(5);
        RequestedChannelMax = 2047;
        MandatoryPublish = false;

        // prefetchCount determines how many messages will be allowed in the local in-memory queue
        // setting to zero makes this infinite, but risks an out-of-memory exception.
        // set to 50 based on this blog post:
        // http://www.rabbitmq.com/blog/2012/04/25/rabbitmq-performance-measurements-part-2/
        PrefetchCount = 50;
        AuthMechanisms = new List<IAuthMechanismFactory> { new PlainMechanismFactory() };

        Hosts = new List<HostConfiguration>();

        Ssl = new SslOption();
        ClientProperties = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Port used to connect to the broker
    /// </summary>
    public ushort Port { get; set; }

    /// <summary>
    ///     Virtual host to connect to
    /// </summary>
    public string VirtualHost { get; set; }

    /// <summary>
    ///     UserName used to connect to the broker
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    ///     Password used to connect to the broker
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    ///     Heartbeat interval (default is 10 seconds)
    /// </summary>
    public TimeSpan RequestedHeartbeat { get; set; }

    /// <summary>
    ///     Prefetch count (default is 50)
    /// </summary>
    public ushort PrefetchCount { get; set; }

    /// <summary>
    /// Dictionary of client properties to be sent to the broker.
    /// You can browse these properties when selecting connection in RabbitMQ Management Plugin.
    /// All properties with <c>null</c> values will be displayed as 'undefined'.
    /// </summary>
    public IDictionary<string, object> ClientProperties { get; }

    /// <summary>
    ///     List of hosts to use for the connection
    /// </summary>
    public IList<HostConfiguration> Hosts { get; set; }

    /// <summary>
    ///     TLS options for the connection.
    /// </summary>
    public SslOption Ssl { get; }

    /// <summary>
    ///     Operations timeout (default is 10s)
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    ///     Enables publisher confirms (default is false)
    /// </summary>
    public bool PublisherConfirms { get; set; }

    /// <summary>
    ///     Enables persistent messages (default is true)
    /// </summary>
    public bool PersistentMessages { get; set; }

    /// <summary>
    ///     Allows to override default product value
    /// </summary>
    public string Product { get; set; }

    /// <summary>
    ///     Allows to override default platform value
    /// </summary>
    public string Platform { get; set; }

    /// <summary>
    ///     Name to be used for connection
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Auth mechanisms to use
    /// </summary>
    public IList<IAuthMechanismFactory> AuthMechanisms { get; set; }

    /// <summary>
    ///     Interval between reconnection attempts. (default is 5s)
    /// </summary>
    public TimeSpan ConnectIntervalAttempt { get; set; }

    /// <summary>
    ///     Enables mandatory flag for publish (default is false)
    /// </summary>
    public bool MandatoryPublish { get; set; }

    /// <summary>
    ///     Maximum channel number per connection (default is 2047)
    /// </summary>
    public ushort RequestedChannelMax { get; set; }

    /// <summary>
    ///     Value greater than one enables concurrent processing for consumers.
    ///     Defaults to 1 so messages are processed in the order they are received; set it explicitly for
    ///     concurrent processing (before 9.0 the default was <seealso cref="PrefetchCount"/>).
    /// </summary>
    /// <remarks>For concurrency greater than one, the consumers could process messages in any order, not in the order they receive them</remarks>
    public ushort? ConsumerDispatcherConcurrency { get; set; } = null;

    public string ClientName { get; set; }

    /// <summary>
    ///     Maximum size in bytes of a message body the client accepts; larger deliveries fail the connection.
    ///     Null uses the RabbitMQ.Client default (64 MiB).
    /// </summary>
    public uint? MaxInboundMessageBodySize { get; set; }

    /// <summary>
    ///     Socket read timeout. Null uses the RabbitMQ.Client default.
    /// </summary>
    public TimeSpan? SocketReadTimeout { get; set; }

    /// <summary>
    ///     Socket write timeout. Null uses the RabbitMQ.Client default.
    /// </summary>
    public TimeSpan? SocketWriteTimeout { get; set; }

    /// <summary>
    ///     Timeout for establishing the initial TCP connection. Null uses the RabbitMQ.Client default.
    /// </summary>
    public TimeSpan? RequestedConnectionTimeout { get; set; }

    /// <summary>
    ///     Timeout for the AMQP handshake continuation. Null uses the RabbitMQ.Client default (10 seconds);
    ///     raise it when TLS or OAuth handshakes need longer.
    /// </summary>
    public TimeSpan? HandshakeContinuationTimeout { get; set; }

    /// <summary>
    ///     Factory for a custom endpoint resolver (custom DNS resolution, endpoint ordering or filtering).
    ///     Null uses the RabbitMQ.Client default resolver over <see cref="Hosts" />.
    /// </summary>
    public Func<IEnumerable<AmqpTcpEndpoint>, IEndpointResolver> EndpointResolverFactory { get; set; }

    /// <summary>
    ///     Credentials provider for dynamic credentials such as OAuth2 tokens; the client refreshes credentials
    ///     via the provider when they expire. When set, it takes precedence over
    ///     <see cref="UserName" />/<see cref="Password" />.
    /// </summary>
    public ICredentialsProvider CredentialsProvider { get; set; }
}

/// <summary>
///     Represents a host configuration
/// </summary>
public class HostConfiguration
{
    public HostConfiguration(string host, ushort port)
    {
        Host = host;
        Port = port;
    }

    /// <summary>
    ///     Address of the host
    /// </summary>
    public string Host { get; }

    /// <summary>
    ///     Port of the host
    /// </summary>
    public ushort Port { get; set; }

    /// <summary>
    ///     TSL configuration of the host
    /// </summary>
    public SslOption Ssl { get; } = new();
}
