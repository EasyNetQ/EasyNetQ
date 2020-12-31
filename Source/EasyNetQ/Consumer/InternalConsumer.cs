using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Logging;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer
{
    /// <summary>
    ///     Represents an internal consumer's status: which queues are consuming and which are not
    /// </summary>
    public readonly struct InternalConsumerStatus
    {
        /// <summary>
        ///     Creates InternalConsumerStatus
        /// </summary>
        public InternalConsumerStatus(IReadOnlyCollection<Queue> succeed, IReadOnlyCollection<Queue> failed)
        {
            Succeed = succeed;
            Failed = failed;
        }

        /// <summary>
        ///     Queues which for which consume is succeed
        /// </summary>
        public IReadOnlyCollection<Queue> Succeed { get; }

        /// <summary>
        ///     Queues which for which consume is failed
        /// </summary>
        public IReadOnlyCollection<Queue> Failed { get; }
    }

    /// <summary>
    ///     Represents an internal consumer's cancelled event
    /// </summary>
    public class InternalConsumerCancelledEventArgs : EventArgs
    {
        /// <inheritdoc />
        public InternalConsumerCancelledEventArgs(Queue cancelled, IReadOnlyCollection<Queue> active)
        {
            Cancelled = cancelled;
            Active = active;
        }

        /// <summary>
        ///     Queue for which consume is cancelled
        /// </summary>
        public Queue Cancelled { get; }

        /// <summary>
        ///     Queues for which consume is active
        /// </summary>
        public IReadOnlyCollection<Queue> Active { get; }
    }

    /// <summary>
    ///     Consumer which starts/stops raw consumers
    /// </summary>
    public interface IInternalConsumer : IDisposable
    {
        /// <summary>
        ///     Starts consuming
        /// </summary>
        InternalConsumerStatus StartConsuming(bool firstStart = true);

        /// <summary>
        ///     Stops consuming
        /// </summary>
        void StopConsuming();

        /// <summary>
        ///     Raised when consumer is cancelled
        /// </summary>
        event EventHandler<InternalConsumerCancelledEventArgs> Cancelled;
    }

    /// <inheritdoc />
    public class InternalConsumer : IInternalConsumer
    {
        private readonly Dictionary<string, AsyncBasicConsumer> consumers = new Dictionary<string, AsyncBasicConsumer>();
        private readonly ILog logger = LogProvider.For<InternalConsumer>();
        private readonly AsyncLock mutex = new AsyncLock();

        private readonly ConsumerConfiguration configuration;
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;

        private volatile bool disposed;
        private volatile IModel model;

        /// <summary>
        ///     Creates InternalConsumer
        /// </summary>
        public InternalConsumer(
            ConsumerConfiguration configuration,
            IPersistentConnection connection,
            IHandlerRunner handlerRunner,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.configuration = configuration;
            this.connection = connection;
            this.handlerRunner = handlerRunner;
            this.eventBus = eventBus;
        }


        /// <inheritdoc />
        public InternalConsumerStatus StartConsuming(bool firstStart)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(InternalConsumer));

            using var _ = mutex.Acquire();

            if (model == null)
            {
                try
                {
                    model = connection.CreateModel();
                    model.BasicQos(0, configuration.PrefetchCount, true);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, "Failed to create model");
                    return new InternalConsumerStatus(Array.Empty<Queue>(), Array.Empty<Queue>());
                }
            }

            var activeQueues = new HashSet<Queue>();
            var failedQueues = new HashSet<Queue>();

            foreach (var kvp in configuration.PerQueueConfigurations)
            {
                var queue = kvp.Key;
                var perQueueConfiguration = kvp.Value;

                if (queue.IsExclusive && !firstStart)
                    continue;

                if (consumers.ContainsKey(queue.Name))
                    continue;

                try
                {
                    var consumer = new AsyncBasicConsumer(
                        model,
                        queue,
                        eventBus,
                        handlerRunner,
                        perQueueConfiguration.Handler
                    );
                    consumer.ConsumerCancelled += AsyncBasicConsumerOnConsumerCancelled;
                    model.BasicConsume(
                        queue.Name, // queue
                        false, // noAck
                        perQueueConfiguration.ConsumerTag, // consumerTag
                        true, // noLocal
                        perQueueConfiguration.IsExclusive, // exclusive
                        perQueueConfiguration.Arguments, // arguments
                        consumer // consumer
                    );
                    consumers.Add(queue.Name, consumer);

                    logger.InfoFormat(
                        "Declared consumer with consumerTag {consumerTag} on queue {queue} and configuration {configuration}",
                        queue.Name,
                        perQueueConfiguration.ConsumerTag,
                        configuration
                    );

                    activeQueues.Add(queue);
                }
                catch (Exception exception)
                {
                    logger.Error(
                        exception,
                        "Consume with consumerTag {consumerTag} on queue {queue} failed",
                        queue.Name,
                        perQueueConfiguration.ConsumerTag
                    );

                    failedQueues.Add(queue);
                }
            }

            return new InternalConsumerStatus(activeQueues, failedQueues);
        }

        /// <inheritdoc />
        public void StopConsuming()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(InternalConsumer));

            using var _ = mutex.Acquire();
            foreach (var consumer in consumers.Values)
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                consumer.Dispose();
            }

            consumers.Clear();
        }

        /// <inheritdoc />
        public event EventHandler<InternalConsumerCancelledEventArgs> Cancelled;

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            using var _ = mutex.Acquire();
            foreach (var consumer in consumers.Values)
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                consumer.Dispose();
            }

            consumers.Clear();
            model?.Dispose();
        }

        private async Task AsyncBasicConsumerOnConsumerCancelled(object sender, ConsumerEventArgs @event)
        {
            Queue cancelled;
            IReadOnlyCollection<Queue> active;
            using (await mutex.AcquireAsync().ConfigureAwait(false))
            {
                if (sender is AsyncBasicConsumer consumer && consumers.Remove(consumer.Queue.Name))
                {
                    consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                    consumer.Dispose();
                    cancelled = consumer.Queue;
                    active = consumers.Select(x => x.Value.Queue).ToList();
                }
                else
                {
                    return;
                }
            }
            Cancelled?.Invoke(this, new InternalConsumerCancelledEventArgs(cancelled, active));
        }
    }
}
