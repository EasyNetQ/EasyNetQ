using EasyNetQ.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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
    private volatile IConnection? initializedConnection;
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
        status = new PersistentConnectionStatus(type, PersistentConnectionState.NotInitialised);
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
    public async Task<IChannel> CreateChannelAsync()
    {
        if (disposed) throw new ObjectDisposedException(nameof(PersistentConnection));

        var connection = InitializeConnection();
        connection.EnsureIsOpen();
        return await connection.CreateChannelAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;

        DisposeConnection();
        disposed = true;
    }

    private IConnection InitializeConnection()
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
            status = status.ToDisconnected(exception.Message);
            throw;
        }

        status = status.ToConnected();
        logger.LogInformation(
            "Connection {type} established to broker {broker}, port {port}",
            type,
            connection.Endpoint.HostName,
            connection.Endpoint.Port
        );
        eventBus.Publish(new ConnectionCreatedEvent(type, connection.Endpoint));
        return connection;
    }

    private IConnection CreateConnection()
    {
        var endpoints = configuration.Hosts.Select(x =>
        {
            var ssl = !x.Ssl.Enabled && configuration.Ssl.Enabled
                ? NewSslForHost(configuration.Ssl, x.Host)
                : x.Ssl;
            return new AmqpTcpEndpoint(x.Host, x.Port, ssl);
        }).ToList();

        if (connectionFactory.CreateConnectionAsync(endpoints) is not IConnection connection)
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
        // these events handlers (except RecoverySucceeded one) are nullified on IConnection.Dispose.
        connection.RecoverySucceeded -= OnConnectionRecovered;

        status = status.ToUnknown();
    }

    private void OnConnectionRecovered(object? sender, EventArgs e)
    {
        status = status.ToConnected();
        var connection = (IConnection)sender!;
        logger.LogInformation(
            "Connection {type} recovered to broker {host}:{port}",
            type,
            connection.Endpoint.HostName,
            connection.Endpoint.Port
        );
        eventBus.Publish(new ConnectionRecoveredEvent(type, connection.Endpoint));
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        status = status.ToDisconnected(e.ToString());
        var connection = (IConnection)sender!;
        logger.LogDebug(
            e.Cause as Exception,
            "Connection {type} disconnected from broker {host}:{port} because of {reason}",
            type,
            connection.Endpoint.HostName,
            connection.Endpoint.Port,
            e.ReplyText
        );
        eventBus.Publish(new ConnectionDisconnectedEvent(type, connection.Endpoint, e.ReplyText));
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        logger.LogInformation("Connection {type} blocked with reason {reason}", type, e.Reason);
        eventBus.Publish(new ConnectionBlockedEvent(type, e.Reason ?? "Unknown reason"));
    }

    private void OnConnectionUnblocked(object? sender, EventArgs e)
    {
        logger.LogInformation("Connection {type} unblocked", type);
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
            ServerName = option.ServerName ?? host,
            Version = option.Version,
        };
}
