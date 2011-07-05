using System;
using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IPersistentConnection : IDisposable
    {
        event Action Connected;
        event Action Disconnected;
        bool IsConnected { get; }
        IModel CreateModel();
        void AddSubscriptionAction(Action subscriptionAction);
    }

    /// <summary>
    /// A connection that attempts to reconnect if the inner connection is closed.
    /// </summary>
    public class PersistentConnection : IPersistentConnection
    {
        private const int connectAttemptIntervalMilliseconds = 200;

        private readonly ConnectionFactory connectionFactory;
        private readonly IEasyNetQLogger logger;
        private IConnection connection;
        private readonly IList<Action> subscribeActions;

        public PersistentConnection(ConnectionFactory connectionFactory, IEasyNetQLogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.logger = logger;
            this.subscribeActions = new List<Action>();

            TryToConnect(null);
        }

        public event Action Connected;
        public event Action Disconnected;

        public IModel CreateModel()
        {
            if(!IsConnected)
            {
                throw new EasyNetQException("Rabbit server is not connected.");
            }
            return connection.CreateModel();
        }

        public void AddSubscriptionAction(Action subscriptionAction)
        {
            subscribeActions.Add(subscriptionAction);

            // TODO: catch amqp connection exception
            if (IsConnected) subscriptionAction();
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

        void TryToConnect(object state)
        {
            if(state != null) ((Timer) state).Dispose();

            logger.DebugWrite("Trying to connect");
            if (disposed) return;
            try
            {
                connection = connectionFactory.CreateConnection();
                connection.ConnectionShutdown += OnConnectionShutdown;

                if (Connected != null) Connected();

                logger.DebugWrite("Re-creating subscribers");
                foreach (var subscribeAction in subscribeActions)
                {
                    subscribeAction();
                }
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
            {
                StartTryToConnect();
            }
        }

        void OnConnectionShutdown(IConnection _, ShutdownEventArgs reason)
        {
            if (disposed) return;
            if (Disconnected != null) Disconnected();

            // try to reconnect and re-subscribe
            logger.DebugWrite("OnConnectionShutdown -> Event fired");

            StartTryToConnect();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (IsConnected) connection.Close();
            if(connection != null) connection.Dispose();
        }
    }
}