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
    }

    /// <summary>
    /// A connection that attempts to reconnect if the inner connection is closed.
    /// </summary>
    public class PersistentConnection : IPersistentConnection
    {
        private const int connectAttemptIntervalMilliseconds = 5000;

        private readonly IConnectionFactory connectionFactory;
        private readonly IEasyNetQLogger logger;
        private IConnection connection;

        public PersistentConnection(IConnectionFactory connectionFactory, IEasyNetQLogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.logger = logger;

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
                logger.InfoWrite("Connected to RabbitMQ. Broker: '{0}', VHost: '{1}'", connectionFactory.HostName, connectionFactory.VirtualHost);
            }
            catch (BrokerUnreachableException brokerUnreachableException)
            {
                logger.ErrorWrite("Failed to connect to Broker: '{0}', VHost: '{1}'. Retrying in {2} ms\n" + 
                    "Check HostName, VirtualHost, Username and Password.\n" +
				    "ExceptionMessage: {3}", 
                    connectionFactory.HostName,
                    connectionFactory.VirtualHost,
                    connectAttemptIntervalMilliseconds,
                    brokerUnreachableException.Message);
                StartTryToConnect();
            }
        }

        void OnConnectionShutdown(IConnection _, ShutdownEventArgs reason)
        {
            if (disposed) return;
            if (Disconnected != null) Disconnected();

            // try to reconnect and re-subscribe
            logger.InfoWrite("Disconnected from RabbitMQ Broker");

            StartTryToConnect();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (connection != null) connection.Dispose();
        }
    }
}