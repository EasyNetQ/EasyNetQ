using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using EasyNetQ.Events;
using EasyNetQ.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ
{
    public interface IPersistentConnection : IDisposable
    {
        bool IsConnected { get; }
        /// <summary>
        /// Initialization method that should be called only once,
        /// usually right after the implementation constructor has run.
        /// </summary>
        void Initialize();
        IModel CreateModel();
    }

    /// <summary>
    /// A connection that attempts to reconnect if the inner connection is closed.
    /// </summary>
    public class PersistentConnection : IPersistentConnection
    {
        private readonly ILog logger = LogProvider.For<PersistentConnection>();
        
        private readonly IConnectionFactory connectionFactory;
        private readonly IEventBus eventBus;
        private readonly object locker = new object();
        private bool initialized;
        private IConnection connection;

        public PersistentConnection(IConnectionFactory connectionFactory, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.connectionFactory = connectionFactory;
            this.eventBus = eventBus;
        }

        public void Initialize()
        {
            lock (locker)
            {
                if (initialized)
                {
                    throw new EasyNetQException("This PersistentConnection has already been initialized.");
                }
                initialized = true;
                TryToConnect(null);
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new EasyNetQException("PersistentConnection: Attempt to create a channel while being disconnected.");
            }

            return connection.CreateModel();
        }
        
        public bool IsConnected => connection != null && connection.IsOpen && !disposed;

        void StartTryToConnect()
        {
            Timer timer = null;
#if !NETFX
            timer = new Timer(delegate { TryToConnect(timer); }, 
                null, connectionFactory.Configuration.ConnectIntervalAttempt, Timeout.InfiniteTimeSpan);
#else
            timer = new Timer(TryToConnect);
            timer.Change(connectionFactory.Configuration.ConnectIntervalAttempt, Timeout.InfiniteTimeSpan);
#endif
        }

        void TryToConnect(object timer)
        {
            ((Timer) timer)?.Dispose();

            logger.Debug("Trying to connect");
            if (disposed) return;

            connectionFactory.Reset();
            do
            {
                try
                {
                    connection = connectionFactory.CreateConnection(); // A possible dispose race condition exists, whereby the Dispose() method may run while this loop is waiting on connectionFactory.CreateConnection() returning a connection.  In that case, a connection could be created and assigned to the connection variable, without it ever being later disposed, leading to app hang on shutdown.  The following if clause guards against this condition and ensures such connections are always disposed.
                    if (disposed)
                    {
                        connection.Dispose();
                        break;
                    }

                    try
                    {
                        OnConnected();
                    }
                    catch
                    {
                        connection.Dispose();
                        throw;
                    }
                    
                    connection.ConnectionShutdown += OnConnectionShutdown;
                    connection.ConnectionBlocked += OnConnectionBlocked;
                    connection.ConnectionUnblocked += OnConnectionUnblocked;

                    logger.InfoFormat(
                        "Connected to broker {broker}, port {port}, vhost {vhost}",
                        connectionFactory.CurrentHost.Host,
                        connectionFactory.CurrentHost.Port,
                        connectionFactory.Configuration.VirtualHost
                    );

                    connectionFactory.Success();
                }
                catch (Exception ex) when (ex is SocketException || ex is BrokerUnreachableException || ex is TimeoutException)
                {
                    LogException(ex);
                }
            } while (!disposed && connectionFactory.Next());

            if (!connectionFactory.Succeeded && !disposed)
            {
                logger.ErrorFormat("Failed to connect to any Broker. Retrying in {connectInterval}", connectionFactory.Configuration.ConnectIntervalAttempt);
                StartTryToConnect();
            }
        }

        void LogException(Exception exception)
        {
            logger.Error(
                exception,
                "Failed to connect to broker {broker}, port {port}, vhost {vhost}",
                connectionFactory.CurrentHost.Host,
                connectionFactory.CurrentHost.Port,
                connectionFactory.Configuration.VirtualHost
            );
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (disposed) return;
            OnDisconnected();

            // try to reconnect and re-subscribe
            logger.InfoFormat("Disconnected from broker");

            TryToConnect(null);
        }

        void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            logger.InfoFormat("Connection blocked with reason {reason}", e.Reason);

            eventBus.Publish(new ConnectionBlockedEvent(e.Reason));
        }

        void OnConnectionUnblocked(object sender, EventArgs e)
        {
            logger.InfoFormat("Connection unblocked");

            eventBus.Publish(new ConnectionUnblockedEvent());
        }

        public void OnConnected()
        {
            logger.Debug("OnConnected event fired");
            eventBus.Publish(new ConnectionCreatedEvent());
        }

        public void OnDisconnected()
        {
            eventBus.Publish(new ConnectionDisconnectedEvent());
        }

        private bool disposed;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (connection != null)
            {
                try
                {
                    connection.Dispose();
                }
                catch (IOException exception)
                {
                    logger.Info(exception, "This is not normally a cause for concern");
                }
            }
        }
    }
}