using System;
using System.Linq;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Persistent
{
    /// <inheritdoc />
    public class PersistentConnection : IPersistentConnection
    {
        private readonly object mutex = new();
        private readonly PersistentConnectionType type;
        private readonly ILogger logger;
        private readonly ConnectionConfiguration configuration;
        private readonly IConnectionFactory connectionFactory;
        private readonly IEventBus eventBus;
        private volatile IAutorecoveringConnection initializedConnection;
        private volatile bool disposed;

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
            Preconditions.CheckNotNull(logger, nameof(logger));
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            Preconditions.CheckNotNull(connectionFactory, nameof(connectionFactory));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));

            this.type = type;
            this.logger = logger;
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

            connection.Dispose();
            // We previously agreed to dispose firstly and then unsubscribe from events so as not to lose logs.
            // These works only for connection.RecoverySucceeded -= OnConnectionRecovered;, for other events
            // it's prohibited to unsubscribe from them after a connection disposal. There are a good news though:
            // these events handlers (except RecoverySucceeded one) are nullified on AutorecoveringConnection.Dispose.
            connection.RecoverySucceeded -= OnConnectionRecovered;
        }

        private void OnConnectionRecovered(object sender, EventArgs e)
        {
            var connection = (IConnection)sender;
            logger.InfoFormat(
                "Connection {type} recovered to broker {host}:{port}",
                type,
                connection.Endpoint.HostName,
                connection.Endpoint.Port
            );
            eventBus.Publish(new ConnectionRecoveredEvent(type, connection.Endpoint));
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            var connection = (IConnection)sender;
            logger.InfoFormat(
                "Connection {type} disconnected from broker {host}:{port} because of {reason}",
                type,
                connection.Endpoint.HostName,
                connection.Endpoint.Port,
                e.ReplyText
            );
            eventBus.Publish(new ConnectionDisconnectedEvent(type, connection.Endpoint, e.ReplyText));
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            logger.InfoFormat("Connection {type} blocked with reason {reason}", type, e.Reason);
            eventBus.Publish(new ConnectionBlockedEvent(type, e.Reason));
        }

        private void OnConnectionUnblocked(object sender, EventArgs e)
        {
            logger.InfoFormat("Connection {type} unblocked", type);
            eventBus.Publish(new ConnectionUnblockedEvent(type));
        }
    }
}
