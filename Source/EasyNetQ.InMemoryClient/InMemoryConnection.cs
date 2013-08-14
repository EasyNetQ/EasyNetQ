using System.Collections;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#pragma warning disable 67

namespace EasyNetQ.InMemoryClient
{
    /// <summary>
    /// An in-memory IConnection
    /// </summary>
    public class InMemoryConnection : IConnection
    {
        private readonly IDictionary<string, ExchangeInfo> exchanges = new Dictionary<string, ExchangeInfo>();
        private readonly IDictionary<string, QueueInfo> queues = new Dictionary<string, QueueInfo>();

        public InMemoryConnection()
        {
            // define the default direct exchange...
            exchanges.Add("", new ExchangeInfo("", "direct", true));
        }

        public IDictionary<string, ExchangeInfo> Exchanges
        {
            get { return exchanges; }
        }

        public IDictionary<string, QueueInfo> Queues
        {
            get { return queues; }
        }

        public void DeleteQueue(string queueName)
        {
            if (queues.ContainsKey(queueName))
            {
                var queueInfo = queues[queueName];
                queues.Remove(queueName);
                queueInfo.FireConsumerCancelNotification();
            }
        }

        public void Dispose()
        {
            // nothing to do.
        }

        public IModel CreateModel()
        {
            return new InMemoryModel(this);
        }

        public void Close()
        {
            // nothing to do.
        }

        public void Close(ushort reasonCode, string reasonText)
        {
            // nothing to do.
        }

        public void Close(int timeout)
        {
            // nothing to do.
        }

        public void Close(ushort reasonCode, string reasonText, int timeout)
        {
            // nothing to do.
        }

        public void Abort()
        {
            // nothing to do.
        }

        public void Abort(ushort reasonCode, string reasonText)
        {
            // nothing to do.
        }

        public void Abort(int timeout)
        {
            // nothing to do.
        }

        public void Abort(ushort reasonCode, string reasonText, int timeout)
        {
            // nothing to do.
        }

        public AmqpTcpEndpoint Endpoint
        {
            get { throw new System.NotImplementedException(); }
        }

        public IProtocol Protocol
        {
            get { throw new System.NotImplementedException(); }
        }

        public ushort ChannelMax
        {
            get { throw new System.NotImplementedException(); }
        }

        public uint FrameMax
        {
            get { throw new System.NotImplementedException(); }
        }

        public ushort Heartbeat
        {
            get { throw new System.NotImplementedException(); }
        }

        public IDictionary ClientProperties
        {
            get { throw new System.NotImplementedException(); }
        }

        public IDictionary ServerProperties
        {
            get { throw new System.NotImplementedException(); }
        }

        public AmqpTcpEndpoint[] KnownHosts
        {
            get { throw new System.NotImplementedException(); }
        }

        public ShutdownEventArgs CloseReason
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool IsOpen
        {
            get { return true; }
        }

        public bool AutoClose
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public IList ShutdownReport
        {
            get { throw new System.NotImplementedException(); }
        }

        public event ConnectionShutdownEventHandler ConnectionShutdown;
        public event CallbackExceptionEventHandler CallbackException;
    }
}

#pragma warning restore 67