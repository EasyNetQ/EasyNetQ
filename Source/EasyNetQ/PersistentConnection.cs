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

                    connectionFactory.Success();
                    
                }
                catch (SocketException socketException)
                {
                    LogException(socketException);
                }
                catch (BrokerUnreachableException brokerUnreachableException)
                {
                    LogException(brokerUnreachableException);
                }
            } while (!disposed && connectionFactory.Next());

            if (connectionFactory.Succeeded)
            {
                connection.ConnectionShutdown += OnConnectionShutdown;
                connection.ConnectionBlocked += OnConnectionBlocked;
                connection.ConnectionUnblocked += OnConnectionUnblocked;

                OnConnected();
                logger.InfoFormat("Connected to RabbitMQ. Broker: '{0}', Port: {1}, VHost: '{2}'",
                    connectionFactory.CurrentHost.Host,
                    connectionFactory.CurrentHost.Port,
                    connectionFactory.Configuration.VirtualHost);
            }
            else
            {
                if (!disposed)
                {
                    logger.InfoFormat("Failed to connect to any Broker. Retrying in {0}",
                        connectionFactory.Configuration.ConnectIntervalAttempt);
                    StartTryToConnect();
                }
            }
        }

        void LogException(Exception exception)
        {
            var exceptionMessage = exception.Message;
            // if there is an inner exception, surface its message since it has more detailed information on why connection failed
            if (exception.InnerException != null)
            {
                exceptionMessage = $"{exceptionMessage} ({exception.InnerException.Message})";
            }

            logger.ErrorFormat("Failed to connect to Broker: '{0}', Port: {1} VHost: '{2}'. " +
                    "ExceptionMessage: '{3}'",
                connectionFactory.CurrentHost.Host,
                connectionFactory.CurrentHost.Port,
                connectionFactory.Configuration.VirtualHost,
                exceptionMessage);
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (disposed) return;
            OnDisconnected();

            // try to reconnect and re-subscribe
            logger.InfoFormat("Disconnected from RabbitMQ Broker");

            TryToConnect(null);
        }

        void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            logger.InfoFormat("Connection blocked. Reason: '{0}'", e.Reason);

            eventBus.Publish(new ConnectionBlockedEvent(e.Reason));
        }

        void OnConnectionUnblocked(object sender, EventArgs e)
        {
            logger.InfoFormat("Connection unblocked.");

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
                    logger.InfoFormat(
                        "IOException thrown on connection dispose. Message: '{0}'. " + 
                        "This is not normally a cause for concern.", 
                        exception.Message);
                }
            }
        }
    }
}