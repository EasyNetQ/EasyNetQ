using System;
using System.Linq;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    /// <summary>
    ///     An abstraction on top of connection which manages its persistence and allows to open channels
    /// </summary>
    public interface IPersistentConnection : IDisposable
    {
        /// <summary>
        ///     True if a connection is connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     Establish a connection
        /// </summary>
        void Connect();

        /// <summary>
        ///     Creates a new channel
        /// </summary>
        /// <returns>New channel</returns>
        IModel CreateModel();
    }

    /// <inheritdoc />
    public class PersistentConnection : IPersistentConnection
    {
        private readonly object mutex = new();
        private readonly ConnectionConfiguration configuration;
        private readonly IConnectionFactory connectionFactory;
        private readonly IEventBus eventBus;
        private readonly ILog logger = LogProvider.For<PersistentConnection>();
        private volatile IAutorecoveringConnection initializedConnection;
        private volatile bool disposed;

        /// <summary>
        ///     Creates PersistentConnection
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="connectionFactory">The connection factory</param>
        /// <param name="eventBus">The event bus</param>
        public PersistentConnection(
            ConnectionConfiguration configuration, IConnectionFactory connectionFactory, IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            Preconditions.CheckNotNull(connectionFactory, nameof(connectionFactory));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));

            this.configuration = configuration;
            this.connectionFactory = connectionFactory;
            this.eventBus = eventBus;
        }


        /// <inheritdoc />
        public bool IsConnected => initializedConnection is { IsOpen: true };

        /// <inheritdoc />
        public void Connect()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(PersistentConnection));

            InitializeConnection();
        }

        /// <inheritdoc />
        public IModel CreateModel()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(PersistentConnection));

            var connection = InitializeConnection();

            if (!connection.IsOpen)
                throw new EasyNetQException(
                    "PersistentConnection: Attempt to create a channel while being disconnected"
                );

            return connection.CreateModel();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            DisposeConnection();
        }

        private IAutorecoveringConnection InitializeConnection()
        {
            var connection = initializedConnection;
            if (connection != null) return connection;

            lock (mutex)
            {
                connection = initializedConnection;
                if (connection != null) return connection;

                connection = initializedConnection = CreateConnection();
            }

            logger.InfoFormat(
                "Connected to broker {broker}, port {port}",
                connection.Endpoint.HostName,
                connection.Endpoint.Port
            );
            eventBus.Publish(new ConnectionCreatedEvent(connection.Endpoint));
            return connection;
        }

        private IAutorecoveringConnection CreateConnection()
        {
            var endpoints = configuration.Hosts.Select(x =>
            {
                var endpoint = new AmqpTcpEndpoint(x.Host, x.Port);
                if (x.Ssl.Enabled)
                    endpoint.Ssl = x.Ssl;
                else if (configuration.Ssl.Enabled)
                    endpoint.Ssl = configuration.Ssl;
                return endpoint;
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
            if (connection == null) return;

            connection.RecoverySucceeded -= OnConnectionRecovered;
            connection.ConnectionUnblocked -= OnConnectionUnblocked;
            connection.ConnectionBlocked -= OnConnectionBlocked;
            connection.ConnectionShutdown -= OnConnectionShutdown;
            connection.Dispose();
        }

        private void OnConnectionRecovered(object sender, EventArgs e)
        {
            var connection = (IConnection)sender;
            logger.InfoFormat(
                "Reconnected to broker {host}:{port}",
                connection.Endpoint.HostName,
                connection.Endpoint.Port
            );
            eventBus.Publish(new ConnectionRecoveredEvent(connection.Endpoint));
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            var connection = (IConnection)sender;
            logger.InfoFormat(
                "Disconnected from broker {host}:{port} because of {reason}",
                connection.Endpoint.HostName,
                connection.Endpoint.Port,
                e.ReplyText
            );
            eventBus.Publish(new ConnectionDisconnectedEvent(connection.Endpoint, e.ReplyText));
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            logger.InfoFormat("Connection blocked with reason {reason}", e.Reason);
            eventBus.Publish(new ConnectionBlockedEvent(e.Reason));
        }

        private void OnConnectionUnblocked(object sender, EventArgs e)
        {
            logger.InfoFormat("Connection unblocked");
            eventBus.Publish(new ConnectionUnblockedEvent());
        }
    }
}
