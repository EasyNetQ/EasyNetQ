using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    public interface IPersistentConnection : IDisposable
    {
        bool IsConnected { get; }
        IModel CreateModel();
    }

    public class PersistentConnection : IPersistentConnection
    {
        private readonly CancellationTokenSource connectCancellation = new CancellationTokenSource();
        private readonly ConnectionConfiguration configuration;
        private readonly IConnectionFactory connectionFactory;
        private readonly IEventBus eventBus;
        private readonly ILog logger = LogProvider.For<PersistentConnection>();
        private volatile IConnection connection;
        private Task connectTask;
        private bool disposed;

        public PersistentConnection(
            ConnectionConfiguration configuration, IConnectionFactory connectionFactory, IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.configuration = configuration;
            this.connectionFactory = connectionFactory;
            this.eventBus = eventBus;
        }

        public IModel CreateModel()
        {
            var connection = this.connection;
            if (connection == null || !connection.IsOpen)
                throw new EasyNetQException("PersistentConnection: Attempt to create a channel while being disconnected.");
            return connection.CreateModel();
        }

        public bool IsConnected
        {
            get
            {
                var connection = this.connection;
                return connection != null && connection.IsOpen;
            }
        }

        public void Dispose()
        {
            if (disposed) return;

            connectCancellation.Cancel();
            connectTask?.ContinueWith(_ => { }).Wait();
            connection?.Dispose();

            disposed = true;
        }

        public void Initialize()
        {
            try
            {
                TryToConnect();
            }
            catch (Exception exception)
            {
                logger.Error(
                    exception,
                    "Failed to connect to any of hosts {hosts} and vhost {vhost}",
                    string.Join(",", configuration.Hosts.Select(x => $"{x.Host}:{x.Port}")),
                    configuration.VirtualHost
                );

                connectTask = Task.Run(StartTryToConnect, connectCancellation.Token);
            }
        }

        private async Task StartTryToConnect()
        {
            while (!connectCancellation.IsCancellationRequested)
            {
                try
                {
                    TryToConnect();
                    return;
                }
                catch (Exception exception)
                {
                    logger.Error(
                        exception,
                        "Failed to connect to any of hosts {hosts} and vhost {vhost}",
                        string.Join(",", configuration.Hosts.Select(x => $"{x.Host}:{x.Port}")),
                        configuration.VirtualHost
                    );
                }

                await Task.Delay(configuration.ConnectIntervalAttempt, connectCancellation.Token).ConfigureAwait(false);
            }
        }

        private void TryToConnect()
        {
            var endpoints = configuration.Hosts.Select(x =>
            {
                var endpoint = new AmqpTcpEndpoint(x.Host, x.Port);
                if (x.Ssl.Enabled)
                    endpoint.Ssl = x.Ssl;
                else if (configuration.Ssl.Enabled)
                    endpoint.Ssl = configuration.Ssl;
                return endpoint;
            }).ToArray();

            connection = connectionFactory.CreateConnection(endpoints);
            connection.ConnectionShutdown += OnConnectionShutdown;
            connection.ConnectionBlocked += OnConnectionBlocked;
            connection.ConnectionUnblocked += OnConnectionUnblocked;

            if (connection is IAutorecoveringConnection recoverable)
                recoverable.RecoverySucceeded += OnConnectionRestored;
            else
                throw new NotSupportedException("Non-recoverable connection is not supported");

            logger.InfoFormat(
                "Connected to broker {broker}, port {port}",
                connection.Endpoint.HostName,
                connection.Endpoint.Port
            );

            eventBus.Publish(new ConnectionCreatedEvent());
        }

        private void OnConnectionRestored(object sender, EventArgs e)
        {
            var connection = (IConnection)sender;
            logger.InfoFormat(
                "Reconnected to broker {broker}, port {port}",
                connection.Endpoint.HostName,
                connection.Endpoint.Port
            );

            eventBus.Publish(new ConnectionCreatedEvent());
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            eventBus.Publish(new ConnectionDisconnectedEvent());
            logger.InfoFormat("Disconnected from broker");
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
