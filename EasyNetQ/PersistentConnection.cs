using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

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
        private readonly ConcurrentBag<Action> subscribeActions;

        public PersistentConnection(ConnectionFactory connectionFactory, IEasyNetQLogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.logger = logger;
            this.subscribeActions = new ConcurrentBag<Action>();

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

            try
            {
                subscriptionAction();
            }
            catch (OperationInterruptedException)
            {
                // Looks like the channel closed between our IsConnected check
                // and the subscription action. Do nothing here, when the 
                // connection comes back, the subcription action will be run then.
            }
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
            if (IsConnected && connection != null) connection.Dispose();
        }
    }
}