using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;

namespace EasyNetQ.Consumer
{
    public class ExclusiveConsumer : IConsumer
    {
        private static readonly TimeSpan RestartConsumingPeriod = TimeSpan.FromSeconds(10);
        private readonly ConsumerConfiguration configuration;

        private readonly IList<IDisposable> disposables = new List<IDisposable>();
        private readonly IEventBus eventBus;

        private readonly IInternalConsumerFactory internalConsumerFactory;

        private readonly ConcurrentSet<IInternalConsumer> internalConsumers = new ConcurrentSet<IInternalConsumer>();
        private readonly MessageHandler onMessage;

        private readonly IQueue queue;

        private readonly object syncLock = new object();

        private bool disposed;
        private volatile bool isStarted;

        public ExclusiveConsumer(
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
            disposables.Add(eventBus.Subscribe<ConnectionRecoveredEvent>(OnConnectionRecovered));
            disposables.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(OnConnectionDisconnected));
            disposables.Add(Timers.Start(StartConsumingInternal, RestartConsumingPeriod, RestartConsumingPeriod));

            StartConsumingInternal();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            eventBus.Publish(new StoppedConsumingEvent(this));

            foreach (var disposal in disposables)
                disposal.Dispose();

            foreach (var internalConsumer in internalConsumers)
                internalConsumer.Dispose();
        }

        private void StartConsumingInternal()
        {
            if (disposed)
                return;

            lock (syncLock)
            {
                if (isStarted)
                    return;

                var internalConsumer = internalConsumerFactory.CreateConsumer();
                internalConsumers.Add(internalConsumer);
                internalConsumer.Cancelled += consumer => Dispose();
                var status = internalConsumer.StartConsuming(queue, onMessage, configuration);
                if (status == StartConsumingStatus.Succeed)
                {
                    isStarted = true;
                    eventBus.Publish(new StartConsumingSucceededEvent(this, queue));
                }
                else
                {
                    eventBus.Publish(new StartConsumingFailedEvent(this, queue));
                    internalConsumer.Dispose();
                    internalConsumers.Remove(internalConsumer);
                }
            }
        }

        private void OnConnectionDisconnected(ConnectionDisconnectedEvent _)
        {
            lock (syncLock)
            {
                isStarted = false;
                internalConsumers.Clear();
                internalConsumerFactory.OnDisconnected();
            }
        }

        private void OnConnectionRecovered(ConnectionRecoveredEvent _) => StartConsumingInternal();
    }
}
