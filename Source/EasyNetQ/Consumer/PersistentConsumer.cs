using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly IConsumerConfiguration configuration;

        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;

        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers = 
            new ConcurrentDictionary<IInternalConsumer, object>();

        private readonly IList<CancelSubscription> eventCancellations = new List<CancelSubscription>();

        private bool shouldRecover = false;

        public PersistentConsumer(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection, 
            IConsumerConfiguration configuration,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.queue = queue;
            this.onMessage = onMessage;
            this.connection = connection;
            this.configuration = configuration;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
        }

        public IDisposable StartConsuming()
        {
            eventCancellations.Add(eventBus.Subscribe<ConnectionCreatedEvent>(e => ConnectionOnConnected()));
            eventCancellations.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(e => ConnectionOnDisconnected()));

            StartConsumingInternal();

            return new ConsumerCancellation(Dispose);
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

            if (shouldRecover && configuration.RecoveryAction != null)
            {
                configuration.RecoveryAction();
            }
            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.TryAdd(internalConsumer, null);

            internalConsumer.Cancelled += consumer => Dispose();
            internalConsumer.ConsumerLost += OnConsumerLost;

            internalConsumer.StartConsuming(
                connection, 
                queue,
                onMessage, 
                configuration
                );
        }

        private void ConnectionOnDisconnected()
        {
            internalConsumerFactory.OnDisconnected();
            internalConsumers.Clear();
            shouldRecover = true; //we need only recovery in case of connection loss, it is not needed to set it subsequently false
        }

        private void OnConsumerLost(IInternalConsumer internalConsumer)
        {
            shouldRecover = true; //we need only recovery in case of connection loss, it is not needed to set it subsequently false
            object temp;
            //We should dispose current internal consumer, as pending messages' ack/nack may shutdown it with unknown delivery tag
            internalConsumers.TryRemove(internalConsumer, out temp);
            internalConsumer.Dispose();
            StartConsumingInternal();
        }

        private void ConnectionOnConnected()
        {
            StartConsumingInternal();
        }

        private bool disposed = false;

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            eventBus.Publish(new StoppedConsumingEvent(this));

            foreach (var cancelSubscription in eventCancellations)
            {
                cancelSubscription();
            }

            foreach (var internalConsumer in internalConsumers.Keys)
            {
                internalConsumer.Dispose();
            }
        }
    }
}