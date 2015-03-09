using System;
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
        private const int connectAttemptIntervalMilliseconds = 5000;

        private readonly ConnectionFactory connectionFactory;
        private readonly IEasyNetQLogger logger;
        private readonly IEventBus eventBus;
        private readonly object locker = new object();
        private bool initialized = false;
        private IConnection connection;

        public PersistentConnection(RabbitMQ.Client.IConnectionFactory connectionFactory, IEasyNetQLogger logger, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            Preconditions.CheckTypeMatches(typeof(ConnectionFactory),connectionFactory,"connectionFactory","Expected type of ConnectionFactory");

            this.connectionFactory = (ConnectionFactory)connectionFactory;
            this.logger = logger;
            this.eventBus = eventBus;
            TryToConnect(null);
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
            if(!IsConnected) throw new EasyNetQException("Not connected");

            return connection.CreateModel();
        }

        public bool IsConnected
        {
            get { return connection != null && connection.IsOpen && !disposed; }
        }

        void StartTryToConnect()
        {
            var timer = new Timer(TryToConnect);
            timer.Change(connectAttemptIntervalMilliseconds, Timeout.Infinite);
        }

        void TryToConnect(object timer)
        {
            if(timer != null) ((Timer) timer).Dispose();

            logger.DebugWrite("Trying to connect");
            if (disposed) return;
            var succeeded = false;
            try
            {
                connection = connectionFactory.CreateConnection();
                succeeded = true;
            }
            catch (System.Net.Sockets.SocketException socketException)
            {
                LogException(socketException);
            }
            catch (BrokerUnreachableException brokerUnreachableException)
            {
                LogException(brokerUnreachableException);
            }

            if (succeeded)
            {
                connection.ConnectionShutdown += OnConnectionShutdown;
                connection.ConnectionBlocked += OnConnectionBlocked;
                connection.ConnectionUnblocked += OnConnectionUnblocked;

                OnConnected();
                logger.InfoWrite("Connected to RabbitMQ. Broker: '{0}', Port: {1}, VHost: '{2}'",
                    connectionFactory.HostName,
                    connectionFactory.Port,
                    connectionFactory.VirtualHost);
            }
            else
            {
                logger.ErrorWrite("Failed to connect to any Broker. Retrying in {0} ms\n", 
                    connectAttemptIntervalMilliseconds);
                StartTryToConnect();
            }
        }

        void LogException(Exception exception)
        {
            logger.ErrorWrite("Failed to connect to Broker: '{0}', Port: {1} VHost: '{2}'. " +
                    "ExceptionMessage: '{3}'",
                    connectionFactory.HostName,
                    connectionFactory.Port,
                    connectionFactory.VirtualHost,
                exception.Message);
        }

        void OnConnectionShutdown(IConnection _, ShutdownEventArgs reason)
        {
            if (disposed) return;
            OnDisconnected();

            // try to reconnect and re-subscribe
            logger.InfoWrite("Disconnected from RabbitMQ Broker");

            TryToConnect(null);
        }

        void OnConnectionBlocked(IConnection sender, ConnectionBlockedEventArgs reason)
        {
            logger.InfoWrite("Connection blocked. Reason: '{0}'", reason.Reason);

            eventBus.Publish(new ConnectionBlockedEvent(reason.Reason));
        }

        void OnConnectionUnblocked(IConnection sender)
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
                catch (System.IO.IOException exception)
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