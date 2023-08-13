using EasyNetQ.Events;
using EasyNetQ.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Persistent;

/// <inheritdoc />
public class PersistentConnection : IPersistentConnection
{
    private readonly object mutex = new();
    private readonly PersistentConnectionType type;
    private readonly ILogger logger;
    private readonly ConnectionConfiguration configuration;
    private readonly IConnectionFactory connectionFactory;
    private readonly IEventBus eventBus;
    private volatile IAutorecoveringConnection? initializedConnection;
    private volatile bool disposed;
    private volatile PersistentConnectionStatus status;

    /// <summary>
    ///     Creates PersistentConnection
    /// </summary>
    public PersistentConnection(
        PersistentConnectionType type,
        ILogger<IPersistentConnection> logger,
        ConnectionConfiguration configuration,
        IConnectionFactory connectionFactory,
        IEventBus eventBus
    )
    {
        this.type = type;
        this.logger = logger;
        this.configuration = configuration;
        this.connectionFactory = connectionFactory;
        this.eventBus = eventBus;
        status = new PersistentConnectionStatus(type, PersistentConnectionState.Unknown);
    }

    /// <inheritdoc />
    public PersistentConnectionStatus Status => status;

    /// <inheritdoc />
    public void EnsureConnected()
    {
        if (disposed) throw new ObjectDisposedException(nameof(PersistentConnection));

        var connection = InitializeConnection();
        connection.EnsureIsOpen();
    }

    /// <inheritdoc />
    public IModel CreateModel()
    {
        if (disposed) throw new ObjectDisposedException(nameof(PersistentConnection));

        var connection = InitializeConnection();
        connection.EnsureIsOpen();
        return connection.CreateModel();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;

        DisposeConnection();
        disposed = true;
    }

    private IAutorecoveringConnection InitializeConnection()
    {
        var connection = initializedConnection;
        if (connection is not null) return connection;

        try
        {
            lock (mutex)
            {
                connection = initializedConnection;
                if (connection is not null) return connection;

                connection = initializedConnection = CreateConnection();
            }
        }
        catch (Exception exception)
        {
            status = status.SwitchToDisconnected(exception.Message);
            throw;
        }

        status = status.SwitchToConnected();
        logger.InfoFormat(
            "Connection {type} established to broker {broker}, port {port}",
            type,
            connection.Endpoint.HostName,
            connection.Endpoint.Port
        );
        eventBus.Publish(new ConnectionCreatedEvent(type, connection.Endpoint));
        return connection;
    }

    private IAutorecoveringConnection CreateConnection()
    {
        var endpoints = configuration.Hosts.Select(x =>
        {
            var ssl = !x.Ssl.Enabled && configuration.Ssl.Enabled
                ? NewSslForHost(configuration.Ssl, x.Host)
                : x.Ssl;
            return new AmqpTcpEndpoint(x.Host, x.Port, ssl);
        }).ToList();

        if (connectionFactory.CreateConnection(endpoints) is not IAutorecoveringConnection connection)
            throw new NotSupportedException("Non-recoverable connection is not supported");

        connection.ConnectionShutdown += OnConnectionShutdown;
        connection.ConnectionBlocked += OnConnectionBlocked;
        connection.ConnectionUnblocked += OnConnectionUnblocked;
        connection.RecoverySucceeded += OnConnectionRecovered;

        return connection;
    }

    private void DisposeConnection()
    {
        var connection = Interlocked.Exchange(ref initializedConnection, null);
        if (connection is null) return;

        connection.Dispose();
        // We previously agreed to dispose firstly and then unsubscribe from events so as not to lose logs.
        // These works only for connection.RecoverySucceeded -= OnConnectionRecovered;, for other events
        // it's prohibited to unsubscribe from them after a connection disposal. There are good news though:
        // these events handlers (except RecoverySucceeded one) are nullified on IAutorecoveringConnection.Dispose.
        connection.RecoverySucceeded -= OnConnectionRecovered;

        status = status.SwitchToUnknown();
    }

    private void OnConnectionRecovered(object? sender, EventArgs e)
    {
        status = status.SwitchToConnected();
        var connection = (IConnection)sender!;
        logger.InfoFormat(
            "Connection {type} recovered to broker {host}:{port}",
            type,
            connection.Endpoint.HostName,
            connection.Endpoint.Port
        );
        eventBus.Publish(new ConnectionRecoveredEvent(type, connection.Endpoint));
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        status = status.SwitchToDisconnected(e.ToString());
        var connection = (IConnection)sender!;
        logger.InfoException(
            "Connection {type} disconnected from broker {host}:{port} because of {reason}",
            e.Cause as Exception,
            type,
            connection.Endpoint.HostName,
            connection.Endpoint.Port,
            e.ReplyText
        );
        eventBus.Publish(new ConnectionDisconnectedEvent(type, connection.Endpoint, e.ReplyText));
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        logger.InfoFormat("Connection {type} blocked with reason {reason}", type, e.Reason);
        eventBus.Publish(new ConnectionBlockedEvent(type, e.Reason ?? "Unknown reason"));
    }

    private void OnConnectionUnblocked(object? sender, EventArgs e)
    {
        logger.InfoFormat("Connection {type} unblocked", type);
        eventBus.Publish(new ConnectionUnblockedEvent(type));
    }

    private static SslOption NewSslForHost(SslOption option, string host) =>
        new()
        {
            Certs = option.Certs,
            AcceptablePolicyErrors = option.AcceptablePolicyErrors,
            CertPassphrase = option.CertPassphrase,
            CertPath = option.CertPath,
            CertificateSelectionCallback = option.CertificateSelectionCallback,
            CertificateValidationCallback = option.CertificateValidationCallback,
            CheckCertificateRevocation = option.CheckCertificateRevocation,
            Enabled = option.Enabled,
            ServerName = host,
            Version = option.Version,
        };
}
