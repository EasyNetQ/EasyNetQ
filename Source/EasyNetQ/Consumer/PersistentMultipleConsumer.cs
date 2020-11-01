using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Consumer
{
    public class PersistentMultipleConsumer : IConsumer
    {
        private readonly ConsumerConfiguration configuration;
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;

        private readonly IInternalConsumerFactory internalConsumerFactory;

        private readonly ConcurrentSet<IInternalConsumer> internalConsumers = new ConcurrentSet<IInternalConsumer>();

        private readonly IReadOnlyCollection<Tuple<IQueue, MessageHandler>> queueConsumerPairs;

        private readonly IList<IDisposable> subscriptions = new List<IDisposable>();

        private bool disposed;

        public PersistentMultipleConsumer(
            IReadOnlyCollection<Tuple<IQueue, MessageHandler>> queueConsumerPairs,
            IPersistentConnection connection,
            ConsumerConfiguration configuration,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus
        )
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

        /// <inheritdoc />
        public void StartConsuming()
        {
            subscriptions.Add(eventBus.Subscribe<ConnectionRecoveredEvent>(OnConnectionRecovered));
            subscriptions.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(OnConnectionDisconnected));

            StartConsumingInternal();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            eventBus.Publish(new StoppedConsumingEvent(this));

            foreach (var subscription in subscriptions)
                subscription.Dispose();

            foreach (var internalConsumer in internalConsumers)
                internalConsumer.Dispose();
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

        private void OnConnectionDisconnected(ConnectionDisconnectedEvent _)
        {
            internalConsumerFactory.OnDisconnected();
            internalConsumers.Clear();
        }

        private void OnConnectionRecovered(ConnectionRecoveredEvent _) => StartConsumingInternal();
    }
}
