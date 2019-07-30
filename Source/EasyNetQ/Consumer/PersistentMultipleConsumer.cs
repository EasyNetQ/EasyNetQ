using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class PersistentMultipleConsumer : IConsumer
    {
        private readonly IConsumerConfiguration configuration;
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;

        private readonly IInternalConsumerFactory internalConsumerFactory;

        private readonly ConcurrentSet<IInternalConsumer> internalConsumers = new ConcurrentSet<IInternalConsumer>();

        private readonly ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task>>> queueConsumerPairs;

        private readonly IList<IDisposable> subscriptions = new List<IDisposable>();

        private bool disposed;

        public PersistentMultipleConsumer(
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task>>> queueConsumerPairs,
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

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            eventBus.Publish(new StoppedConsumingEvent(this));

            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }

            foreach (var internalConsumer in internalConsumers)
            {
                internalConsumer.Dispose();
            }
        }

        private void StartConsumingInternal()
        {
            if (disposed) return;

            if (!connection.IsConnected)
            {
                return;
            }

            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.Add(internalConsumer);

            internalConsumer.Cancelled += consumer => Dispose();

            internalConsumer.StartConsuming(
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
    }
}
