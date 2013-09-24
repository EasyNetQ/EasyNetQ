using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class PersistentConsumer : IConsumer
    {
        private readonly IQueue queue;
        private readonly Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;

        private readonly IInternalConsumerFactory internalConsumerFactory;

        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers = 
            new ConcurrentDictionary<IInternalConsumer, object>();

        public event Action<IConsumer> RemoveMeFromList;

        public PersistentConsumer(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection, 
            IInternalConsumerFactory internalConsumerFactory)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");

            this.queue = queue;
            this.onMessage = onMessage;
            this.connection = connection;
            this.internalConsumerFactory = internalConsumerFactory;
        }

        public void StartConsuming()
        {
            connection.Connected += ConnectionOnConnected;
            connection.Disconnected += ConnectionOnDisconnected;

            StartConsumingInternal();
        }

        private void StartConsumingInternal()
        {
            if(!connection.IsConnected)
            {
                // connection is not connected, so just ignore this call. A consumer will
                // be created and start consuming when the connection reconnects.
            }

            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.TryAdd(internalConsumer, null);

            internalConsumer.Cancelled += consumer =>
                {
                    if (disposed) return;

                    object value; // cruft from using a ConcurrentDictionary
                    internalConsumers.TryRemove(consumer, out value);
                    StartConsumingInternal();
                };

            internalConsumer.StartConsuming(
                connection, 
                queue,
                onMessage);
        }

        private void ConnectionOnDisconnected()
        {
            internalConsumers.Clear();
        }

        private void ConnectionOnConnected()
        {
            StartConsumingInternal();
        }

        private bool disposed = false;

        public void Dispose()
        {
            disposed = true;

            connection.Connected -= ConnectionOnConnected;
            connection.Disconnected -= ConnectionOnDisconnected;

            foreach (var internalConsumer in internalConsumers.Keys)
            {
                internalConsumer.Dispose();
            }
        }
    }
}