using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class PersistentConsumer : IConsumer
    {
        private readonly IQueue queue;
        private readonly Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;

        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;

        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers = 
            new ConcurrentDictionary<IInternalConsumer, object>();

        public event Action<IConsumer> RemoveMeFromList;

        public PersistentConsumer(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection, 
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.queue = queue;
            this.onMessage = onMessage;
            this.connection = connection;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
        }

        public void StartConsuming()
        {
            eventBus.Subscribe<ConnectionCreatedEvent>(e => ConnectionOnConnected());
            eventBus.Subscribe<ConnectionDisconnectedEvent>(e => ConnectionOnDisconnected());

            StartConsumingInternal();
        }

        private void StartConsumingInternal()
        {
            if (disposed) return;

            if(!connection.IsConnected)
            {
                // connection is not connected, so just ignore this call. A consumer will
                // be created and start consuming when the connection reconnects.
                return;
            }

            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.TryAdd(internalConsumer, null);

            internalConsumer.Cancelled += consumer =>
                {
//                    Console.Out.WriteLine(">>>>>>>> internalConsumer.Cancelled");
//                    if (disposed) return;
//
//                    object value; // cruft from using a ConcurrentDictionary
//                    internalConsumers.TryRemove(consumer, out value);
//                    StartConsumingInternal();
                };

            internalConsumer.StartConsuming(
                connection, 
                queue,
                onMessage);
        }

        private void ConnectionOnDisconnected()
        {
            internalConsumerFactory.OnDisconnected();
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

            foreach (var internalConsumer in internalConsumers.Keys)
            {
                internalConsumer.Dispose();
            }
        }
    }
}