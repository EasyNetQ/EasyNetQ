﻿using System;
using System.Collections.Generic;
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
        public InternalConsumerStatus(IReadOnlyCollection<IQueue> succeed, IReadOnlyCollection<IQueue> failed)
        {
            Succeed = succeed;
            Failed = failed;
        }

        /// <summary>
        ///     Queues which for which consume is succeed
        /// </summary>
        public IReadOnlyCollection<IQueue> Succeed { get; }

        /// <summary>
        ///     Queues which for which consume is failed
        /// </summary>
        public IReadOnlyCollection<IQueue> Failed { get; }
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
        ///     Raised when consumer is completely cancelled
        /// </summary>
        event EventHandler<EventArgs> Cancelled;
    }

    /// <inheritdoc />
    public class InternalConsumer : IInternalConsumer
    {
        private readonly ConsumerConfiguration configuration;
        private readonly IPersistentConnection connection;

        private readonly Dictionary<IQueue, AsyncBasicConsumer>
            consumers = new Dictionary<IQueue, AsyncBasicConsumer>();

        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;
        private readonly ILog logger = LogProvider.For<InternalConsumer>();
        private readonly AsyncLock mutex = new AsyncLock();

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
                    return new InternalConsumerStatus(Array.Empty<IQueue>(), Array.Empty<IQueue>());
                }
            }

            var activeQueues = new HashSet<IQueue>();
            var failedQueues = new HashSet<IQueue>();

            foreach (var kvp in configuration.PerQueueConfigurations)
            {
                var queue = kvp.Key;
                var perQueueConfiguration = kvp.Value;

                if (queue.IsExclusive && !firstStart)
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
        public event EventHandler<EventArgs> Cancelled;

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
            using var _ = await mutex.AcquireAsync().ConfigureAwait(false);

            if (sender is AsyncBasicConsumer consumer && consumers.Remove(consumer.Queue))
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                consumer.Dispose();
                if (consumers.Count == 0)
                    Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
