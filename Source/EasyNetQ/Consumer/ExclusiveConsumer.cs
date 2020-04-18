using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class ExclusiveConsumer : IConsumer
    {
        private static readonly TimeSpan RestartConsumingPeriod = TimeSpan.FromSeconds(10);
        private readonly IConsumerConfiguration configuration;

        private readonly IList<IDisposable> disposables = new List<IDisposable>();
        private readonly IEventBus eventBus;

        private readonly IInternalConsumerFactory internalConsumerFactory;

        private readonly ConcurrentSet<IInternalConsumer> internalConsumers = new ConcurrentSet<IInternalConsumer>();
        private readonly Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage;

        private readonly IQueue queue;

        private readonly object syncLock = new object();

        private bool disposed;
        private volatile bool isStarted;

        public ExclusiveConsumer(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage,
            IConsumerConfiguration configuration,
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

        public IDisposable StartConsuming()
        {
            disposables.Add(eventBus.Subscribe<ConnectionCreatedEvent>(e => ConnectionOnConnected()));
            disposables.Add(eventBus.Subscribe<ConnectionDisconnectedEvent>(e => ConnectionOnDisconnected()));
            disposables.Add(Timers.Start(StartConsumingInternal, RestartConsumingPeriod, RestartConsumingPeriod));

            StartConsumingInternal();

            return new ConsumerCancellation(Dispose);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            eventBus.Publish(new StoppedConsumingEvent(this));

            foreach (var disposal in disposables)
            {
                disposal.Dispose();
            }

            foreach (var internalConsumer in internalConsumers)
            {
                internalConsumer.Dispose();
            }
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

        private void ConnectionOnDisconnected()
        {
            lock (syncLock)
            {
                isStarted = false;
                internalConsumers.Clear();
                internalConsumerFactory.OnDisconnected();
            }
        }

        private void ConnectionOnConnected()
        {
            StartConsumingInternal();
        }
    }
}
