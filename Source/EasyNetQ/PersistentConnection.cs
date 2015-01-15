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
        IModel CreateModel();
    }

    /// <summary>
    /// A connection that attempts to reconnect if the inner connection is closed.
    /// </summary>
    public class PersistentConnection : IPersistentConnection
    {
        private const int connectAttemptIntervalMilliseconds = 5000;

        private readonly IConnectionFactory connectionFactory;
        private readonly IEasyNetQLogger logger;
        private readonly IEventBus eventBus;
        private IConnection connection;

        public PersistentConnection(IConnectionFactory connectionFactory, IEasyNetQLogger logger, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connectionFactory, "connectionFactory");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.connectionFactory = connectionFactory;
            this.logger = logger;
            this.eventBus = eventBus;

            TryToConnect(null);
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

            connectionFactory.Reset();
            do
            {
                try
                {
                    connection = connectionFactory.CreateConnection();
                    connectionFactory.Success();
                }
                catch (System.Net.Sockets.SocketException socketException)
                {
                    LogException(socketException);
                }
                catch (BrokerUnreachableException brokerUnreachableException)
                {
                    LogException(brokerUnreachableException);
                }
            } while (connectionFactory.Next());

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
                logger.ErrorWrite("Failed to connect to any Broker. Retrying in {0} ms\n", 
                    connectAttemptIntervalMilliseconds);
                StartTryToConnect();
            }
        }

        void LogException(Exception exception)
        {
            logger.ErrorWrite("Failed to connect to Broker: '{0}', Port: {1} VHost: '{2}'. " +
                    "ExceptionMessage: '{3}'",
                connectionFactory.CurrentHost.Host,
                connectionFactory.CurrentHost.Port,
                connectionFactory.Configuration.VirtualHost,
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