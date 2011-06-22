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
        private readonly ConnectionFactory connectionFactory;
        private IConnection connection;
        private readonly IList<Action> subscribeActions;

        public PersistentConnection(ConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
            this.subscribeActions = new List<Action>();

            ConnectIfNotConnected();
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
            if (IsConnected) subscriptionAction();
            subscribeActions.Add(subscriptionAction);
        }

        public bool IsConnected
        {
            get { return connection != null && connection.IsOpen && !disposed; }
        }

        void ConnectIfNotConnected()
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                while (connection == null || !connection.IsOpen)
                {
                    try
                    {
                        connection = connectionFactory.CreateConnection();
                        connection.ConnectionShutdown += OnConnectionShutdown;

                        if (Connected != null) Connected();
                    }
                    catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
                    {
                        // try again a little later
                        Thread.Sleep(100);
                    }
                }
                Console.WriteLine("Re-creating subscribers");
                foreach (var subscribeAction in subscribeActions)
                {
                    subscribeAction();
                }
            });
        }

        void OnConnectionShutdown(IConnection _, ShutdownEventArgs reason)
        {
            if (disposed) return;
            if (Disconnected != null) Disconnected();

            // try to reconnect and re-subscribe
            Console.WriteLine("OnConnectionShutdown -> Event fired");

            Thread.Sleep(100);
            ConnectIfNotConnected();
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