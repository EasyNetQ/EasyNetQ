using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class PersistentMultipleConsumer : IConsumer
    {
        private readonly ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs;
        private readonly IPersistentConnection connection;
        private readonly IConsumerConfiguration configuration;

        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;

        private readonly ConcurrentDictionary<IInternalConsumer, object> internalConsumers =
            new ConcurrentDictionary<IInternalConsumer, object>();

        private readonly IList<IDisposable> subscriptions = new List<IDisposable>();

        public PersistentMultipleConsumer(
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs,
            IPersistentConnection connection,
            IConsumerConfiguration configuration,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(queueConsumerPairs, nameof(queueConsumerPairs));
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(internalConsumerFactory, nameof(internalConsumerFactory));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            this.queueConsumerPairs = queueConsumerPairs;
            this.connection = connection;
            this.configuration = configuration;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
        }

        public IDisposable StartConsuming()
        {
            subscriptions.Add(eventBus.Subscribe<ConnectionCreatedEvent>(e => ConnectionOnConnected()));
            subscriptions.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(e => ConnectionOnDisconnected()));

            StartConsumingInternal();

            return new ConsumerCancellation(Dispose);
        }

        private void StartConsumingInternal()
        {
            if (disposed) return;

            if (!connection.IsConnected)
            {
                return;
            }

            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.TryAdd(internalConsumer, null);

            internalConsumer.Cancelled += consumer => Dispose();

            internalConsumer.StartConsuming(
                connection,
                queueConsumerPairs,
                configuration
            );
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

        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
            
            eventBus.Publish(new StoppedConsumingEvent(this));
            
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }

            foreach (var internalConsumer in internalConsumers.Keys)
            {
                internalConsumer.Dispose();
            }
        }
    }
}