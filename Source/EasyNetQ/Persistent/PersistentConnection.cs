using EasyNetQ.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Persistent;

/// <inheritdoc />
public class PersistentConnection : IPersistentConnection
{
    private readonly PersistentConnectionType type;
    private readonly ILogger logger;
    private readonly ConnectionConfiguration configuration;
    private readonly IConnectionFactory connectionFactory;
    private readonly IEventBus eventBus;
    private volatile IConnection initializedConnection;
    private volatile bool disposed;
    private volatile PersistentConnectionStatus status;
    private SemaphoreSlim mutex = new SemaphoreSlim(1);
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

    public bool IsConnected => initializedConnection is { IsOpen: true };
    /// <inheritdoc />
    public PersistentConnectionStatus Status => status;

    /// <inheritdoc />
    public async Task ConnectAsync()
    {
        if (disposed) throw new ObjectDisposedException(nameof(PersistentConnection));

        var connection = await InitializeConnectionAsync();
        connection.EnsureIsOpen();
    }

    /// <inheritdoc />
    public async Task<IChannel> CreateChannelAsync(
        CreateChannelOptions options = null,
        CancellationToken cancellationToken = default
        )
    {
        if (disposed) throw new ObjectDisposedException(nameof(PersistentConnection));
        var connection = await InitializeConnectionAsync();
        connection.EnsureIsOpen();
        return await connection.CreateChannelAsync(options, cancellationToken);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (disposed) return;

        DisposeConnection();
        mutex.Dispose();
        disposed = true;
    }

    private async Task<IConnection> InitializeConnectionAsync()
    {
        var connection = initializedConnection;
        if (connection is not null) return connection;

        try
        {
            bool lockAcquired = await mutex.WaitAsync(5_000);
            if (!lockAcquired)
                throw new NotSupportedException("Non-recoverable connection is not supported");
            try
            {
                connection = initializedConnection;
                if (connection is not null) return connection;
                connection = initializedConnection = await CreateConnectionAsync(CancellationToken.None);
            }
            finally
            {
                mutex.Release();
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
        await eventBus.PublishAsync(new ConnectionCreatedEvent(type, connection.Endpoint));
        return connection;
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var endpoints = configuration.Hosts.Select(x =>
        {
            var ssl = !x.Ssl.Enabled && configuration.Ssl.Enabled
                ? NewSslForHost(configuration.Ssl, x.Host)
                : x.Ssl;
            return new AmqpTcpEndpoint(x.Host, x.Port, ssl);
        }).ToList();

        if (await connectionFactory.CreateConnectionAsync(endpoints, cancellationToken) is not IConnection connection)
            // review this: the return type is IConnection, so this code is practically unreachable, check with pliner
            throw new NotSupportedException("Non-recoverable connection is not supported");

        connection.ConnectionShutdownAsync += OnConnectionShutdown;
        connection.ConnectionBlockedAsync += OnConnectionBlocked;
        connection.ConnectionUnblockedAsync += OnConnectionUnblocked;
        connection.RecoverySucceededAsync += OnConnectionRecovered;

        return connection;
    }

    private void DisposeConnection()
    {
        var connection = Interlocked.Exchange(ref initializedConnection, null);
        if (connection is null) return;
        // We previously agreed to dispose firstly and then unsubscribe from events so as not to lose logs.
        // These works only for connection.RecoverySucceeded -= OnConnectionRecovered;, for other events
        // it's prohibited to unsubscribe from them after a connection disposal. There are good news though:
        // these events handlers (except RecoverySucceeded one) are nullified on IConnection.Dispose.
        connection.RecoverySucceededAsync -= OnConnectionRecovered;
        connection.Dispose();

        status = status.ToUnknown();
    }

    private async Task OnConnectionRecovered(object sender, AsyncEventArgs e)
    {
        status = status.ToConnected();
        var connection = (IConnection)sender!;
        logger.LogInformation(
            "Connection {type} recovered to broker {host}:{port}",
            type,
            connection.Endpoint.HostName,
            connection.Endpoint.Port
        );
        await eventBus.PublishAsync(new ConnectionRecoveredEvent(type, connection.Endpoint));
    }

    private async Task OnConnectionShutdown(object sender, ShutdownEventArgs e)
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
        await eventBus.PublishAsync(new ConnectionDisconnectedEvent(type, connection.Endpoint, e.ReplyText));
    }

    private async Task OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        logger.LogInformation("Connection {type} blocked with reason {reason}", type, e.Reason);
        await eventBus.PublishAsync(new ConnectionBlockedEvent(type, e.Reason ?? "Unknown reason"));
    }

    private async Task OnConnectionUnblocked(object sender, AsyncEventArgs e)
    {
        logger.LogInformation("Connection {type} unblocked", type);
        await eventBus.PublishAsync(new ConnectionUnblockedEvent(type));
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
