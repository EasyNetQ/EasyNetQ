using EasyNetQ.Events;
using EasyNetQ.Topology;
using System;
using System.Collections.Generic;
using EasyNetQ.Internals;

namespace EasyNetQ.Consumer
{
    /// <summary>
    ///     Represent an abstract consumer
    /// </summary>
    public interface IConsumer : IDisposable
    {
        /// <summary>
        ///     Unique consumer id
        /// </summary>
        Guid Id { get; }

        /// <summary>
        ///     Starts the consumer
        /// </summary>
        /// <returns>Disposable to stop the consumer</returns>
        void StartConsuming();
    }

    /// <summary>
    ///     Configuration of the consumer for a queue
    /// </summary>
    public class PerQueueConsumerConfiguration
    {
        /// <summary>
        ///     Creates PerQueueConsumerConfiguration
        /// </summary>
        public PerQueueConsumerConfiguration(
            string consumerTag,
            bool isExclusive,
            IDictionary<string, object> arguments,
            MessageHandler handler
        )
        {
            ConsumerTag = consumerTag;
            IsExclusive = isExclusive;
            Arguments = arguments;
            Handler = handler;
        }

        /// <summary>
        ///     Tag of the consumer
        /// </summary>
        public string ConsumerTag { get; }
        public bool IsExclusive { get; }
        public IDictionary<string, object> Arguments { get; }
        public MessageHandler Handler { get; }
    }

    public class ConsumerConfiguration
    {
        public ConsumerConfiguration(ushort prefetchCount, IReadOnlyDictionary<IQueue, PerQueueConsumerConfiguration> perQueueConfigurations)
        {
            PrefetchCount = prefetchCount;
            PerQueueConfigurations = perQueueConfigurations;
        }

        public ushort PrefetchCount { get; }
        public IReadOnlyDictionary<IQueue, PerQueueConsumerConfiguration> PerQueueConfigurations { get; }
    }

    /// <inheritdoc />
    public class Consumer : IConsumer
    {
        private static readonly TimeSpan RestartConsumingPeriod = TimeSpan.FromSeconds(10);

        private readonly ConsumerConfiguration configuration;
        private readonly IEventBus eventBus;
        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IDisposable[] disposables;
        private readonly object mutex = new object();

        private volatile IInternalConsumer consumer;
        private volatile bool disposed;

        /// <summary>
        ///     Creates Consumer
        /// </summary>
        public Consumer(
            ConsumerConfiguration configuration,
            IPersistentConnection connection,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(internalConsumerFactory, nameof(internalConsumerFactory));
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            this.configuration = configuration;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
            disposables = new[]
            {
                eventBus.Subscribe<ConnectionRecoveredEvent>(OnConnectionRecovered),
                eventBus.Subscribe<ConnectionDisconnectedEvent>(OnConnectionDisconnected),
                Timers.Start(RestartConsumingPeriodically, RestartConsumingPeriod, RestartConsumingPeriod),
            };
        }

        /// <inheritdoc />
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public void StartConsuming()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(Consumer));

            lock (mutex)
            {
                if (consumer != null)
                    throw new InvalidOperationException("Consumer has already started");

                consumer = internalConsumerFactory.CreateConsumer(configuration);
                consumer.Cancelled += InternalConsumerOnCancelled;

                var status = consumer.StartConsuming();
                foreach (var queue in status.Succeed)
                    eventBus.Publish(new StartConsumingSucceededEvent(this, queue));
                foreach (var queue in status.Failed)
                    eventBus.Publish(new StartConsumingFailedEvent(this, queue));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            foreach (var disposable in disposables)
                disposable.Dispose();
            consumer?.Dispose();

            eventBus.Publish(new StoppedConsumingEvent(this));
        }

        private void InternalConsumerOnCancelled(object sender, InternalConsumerCancelledEventArgs e)
        {
            if (e.Active.Count == 0)
                Dispose();
        }

        private void OnConnectionDisconnected(ConnectionDisconnectedEvent _)
        {
            consumer?.StopConsuming();
        }

        private void OnConnectionRecovered(ConnectionRecoveredEvent _)
        {
            var consumerToRestart = consumer;
            if (consumerToRestart == null)
                return;

            var status = consumerToRestart.StartConsuming(false);
            foreach (var queue in status.Succeed)
                eventBus.Publish(new StartConsumingSucceededEvent(this, queue));
            foreach (var queue in status.Failed)
                eventBus.Publish(new StartConsumingFailedEvent(this, queue));
        }

        private void RestartConsumingPeriodically()
        {
            consumer?.StartConsuming(false);
        }
    }
}
