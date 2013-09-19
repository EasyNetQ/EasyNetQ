using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class PersistentConsumer : IConsumer
    {
        private readonly IQueue queue;
        private readonly Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;

        private readonly IInternalConsumerFactory internalConsumerFactory;

        public bool ModelIsSingleUse { get { return queue.IsSingleUse; } }

        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers = 
            new ConcurrentDictionary<IInternalConsumer, object>();

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
            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.TryAdd(internalConsumer, null);

            object value; // cruft from using a ConcurrentDictionary
            internalConsumer.Cancelled += consumer => internalConsumers.TryRemove(consumer, out value);

            internalConsumer.StartConsuming(
                connection, 
                queue,
                onMessage);
        }

        private void ConnectionOnDisconnected()
        {
        }

        private void ConnectionOnConnected()
        {
            StartConsumingInternal();
        }

        public void Dispose()
        {
            connection.Connected -= ConnectionOnConnected;
            connection.Disconnected -= ConnectionOnDisconnected;

            foreach (var internalConsumer in internalConsumers.Keys)
            {
                internalConsumer.Dispose();
            }
        }
    }
}