using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using EasyNetQ.Events;
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
        private readonly IConnectionFactory connectionFactory;
        private readonly IEasyNetQLogger logger;
        private readonly IEventBus eventBus;
        private readonly object locker = new object();
        private bool initialized = false;
        private IConnection connection;

        public PersistentConnection(IConnectionFactory connectionFactory, IEasyNetQLogger logger, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.connectionFactory = connectionFactory;
            this.logger = logger;
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
        
        public bool IsConnected
        {
            get { return connection != null && connection.IsOpen && !disposed; }
        }

        void StartTryToConnect()
        {
            var timer = new Timer(TryToConnect);
            timer.Change(connectionFactory.Configuration.ConnectIntervalAttempt, Timeout.InfiniteTimeSpan);
        }

        void TryToConnect(object timer)
        {
            if(timer != null) ((Timer) timer).Dispose();

            logger.DebugWrite("Trying to connect");
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
                logger.InfoWrite("Connected to RabbitMQ. Broker: '{0}', Port: {1}, VHost: '{2}'",
                    connectionFactory.CurrentHost.Host,
                    connectionFactory.CurrentHost.Port,
                    connectionFactory.Configuration.VirtualHost);
            }
            else
            {
                if (!disposed)
                {
                    logger.ErrorWrite("Failed to connect to any Broker. Retrying in {0}",
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

            logger.ErrorWrite("Failed to connect to Broker: '{0}', Port: {1} VHost: '{2}'. " +
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
            logger.InfoWrite("Disconnected from RabbitMQ Broker");

            TryToConnect(null);
        }

        void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            logger.InfoWrite("Connection blocked. Reason: '{0}'", e.Reason);

            eventBus.Publish(new ConnectionBlockedEvent(e.Reason));
        }

        void OnConnectionUnblocked(object sender, EventArgs e)
        {
            logger.InfoWrite("Connection unblocked.");

            eventBus.Publish(new ConnectionUnblockedEvent());
        }

        public void OnConnected()
        {
            logger.DebugWrite("OnConnected event fired");
            eventBus.Publish(new ConnectionCreatedEvent());
        }

        public void OnDisconnected()
        {
            eventBus.Publish(new ConnectionDisconnectedEvent());
        }

        private bool disposed = false;
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
                    logger.DebugWrite(
                        "IOException thrown on connection dispose. Message: '{0}'. " + 
                        "This is not normally a cause for concern.", 
                        exception.Message);
                }
            }
        }
    }
}