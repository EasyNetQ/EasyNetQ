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
    public readonly struct InternalConsumerStatus
    {
        public InternalConsumerStatus(IReadOnlyCollection<IQueue> succeed, IReadOnlyCollection<IQueue> failed)
        {
            Succeed = succeed;
            Failed = failed;
        }

        public IReadOnlyCollection<IQueue> Succeed { get; }
        public IReadOnlyCollection<IQueue> Failed { get; }
    }

    public interface IInternalConsumer : IDisposable
    {
        InternalConsumerStatus StartConsuming(bool firstStart = true);
        void StopConsuming();
        event EventHandler<EventArgs> Cancelled;
    }

    public class InternalConsumer : IInternalConsumer
    {
        private readonly ILog logger = LogProvider.For<InternalConsumer>();

        private readonly ConsumerConfiguration configuration;
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;
        private readonly Dictionary<IQueue, AsyncBasicConsumer> consumers = new Dictionary<IQueue, AsyncBasicConsumer>();
        private readonly AsyncLock mutex = new AsyncLock();

        private volatile bool disposed;
        private volatile IModel model;

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
        public void StopConsuming()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(InternalConsumer));

            using var _ = mutex.Acquire();
            foreach (var consumer in consumers.Select(x => x.Value))
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                consumer.Dispose();
            }
            consumers.Clear();
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs> Cancelled;

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
                    return new InternalConsumerStatus(Array.Empty<IQueue>(), Array.Empty<IQueue>());
                }
            }

            var activeQueues = new HashSet<IQueue>();
            var failedQueues = new HashSet<IQueue>();

            foreach (var kvp in configuration.PerQueueConfigurations)
            {
                var queue = kvp.Key;
                var perQueueConfiguration = kvp.Value;

                if (queue.IsAutoDelete && !firstStart)
                    continue;

                if (consumers.ContainsKey(queue))
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
                    consumers.Add(queue, consumer);

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

        private async Task AsyncBasicConsumerOnConsumerCancelled(object sender, ConsumerEventArgs @event)
        {
            using var _ = await mutex.AcquireAsync().ConfigureAwait(false);

            if (sender is AsyncBasicConsumer consumer && consumers.Remove(consumer.Queue))
            {
                consumer.Dispose();
                if (consumers.Count == 0)
                    Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            using var _ = mutex.Acquire();
            foreach (var consumer in consumers.Select(x => x.Value))
                consumer.Dispose();
            model?.Dispose();
        }
    }
}
