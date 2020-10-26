using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class PersistentConsumer : IConsumer
    {
        private readonly ConsumerConfiguration configuration;
        private readonly IEventBus eventBus;

        private readonly IInternalConsumerFactory internalConsumerFactory;

        private readonly ConcurrentSet<IInternalConsumer> internalConsumers = new ConcurrentSet<IInternalConsumer>();

        private readonly MessageHandler onMessage;
        private readonly IQueue queue;

        private readonly IList<IDisposable> subscriptions = new List<IDisposable>();

        private bool disposed;

        public PersistentConsumer(
            IQueue queue,
            MessageHandler onMessage,
            ConsumerConfiguration configuration,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.queue = queue;
            this.onMessage = onMessage;
            this.configuration = configuration;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
        }

        /// <inheritdoc />
        public void StartConsuming()
        {
            subscriptions.Add(eventBus.Subscribe<ConnectionRecoveredEvent>(ConnectionOnConnected));
            subscriptions.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(ConnectionOnDisconnected));

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

            var internalConsumer = internalConsumerFactory.CreateConsumer();
            internalConsumers.Add(internalConsumer);

            internalConsumer.Cancelled += consumer => Dispose();

            var status = internalConsumer.StartConsuming(
                queue,
                onMessage,
                configuration
            );

            if (status == StartConsumingStatus.Succeed)
                eventBus.Publish(new StartConsumingSucceededEvent(this, queue));
            else
                eventBus.Publish(new StartConsumingFailedEvent(this, queue));
        }

        private void ConnectionOnDisconnected(ConnectionDisconnectedEvent _)
        {
            internalConsumerFactory.OnDisconnected();
        }

        private void ConnectionOnConnected(ConnectionRecoveredEvent _)
        {
            if (disposed) return;

            foreach (var internalConsumer in internalConsumers)
            {
                var status = internalConsumer.StartConsuming(
                    queue,
                    onMessage,
                    configuration
                );

                if (status == StartConsumingStatus.Succeed)
                    eventBus.Publish(new StartConsumingSucceededEvent(this, queue));
                else
                    eventBus.Publish(new StartConsumingFailedEvent(this, queue));
            }
        }
    }
}
