using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Logging;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer
{
    public interface IInternalConsumer : IDisposable
    {
        StartConsumingStatus StartConsuming(
            IQueue queue,
            MessageHandler onMessage,
            ConsumerConfiguration configuration
        );

        StartConsumingStatus StartConsuming(
            IReadOnlyCollection<Tuple<IQueue, MessageHandler>> queueConsumerPairs,
            ConsumerConfiguration configuration
        );

        event Action<IInternalConsumer> Cancelled;
    }

    public class BasicConsumer : IBasicConsumer, IDisposable
    {
        private readonly Action<BasicConsumer> cancelled;
        private readonly IConsumerDispatcher consumerDispatcher;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;
        private readonly ILog logger = LogProvider.For<BasicConsumer>();

        private bool disposed;

        public BasicConsumer(
            Action<BasicConsumer> cancelled,
            IConsumerDispatcher consumerDispatcher,
            IQueue queue,
            IEventBus eventBus,
            IHandlerRunner handlerRunner,
            MessageHandler onMessage,
            IModel model
        )
        {
            Preconditions.CheckNotNull(onMessage, "onMessage");

            Queue = queue;
            OnMessage = onMessage;
            this.cancelled = cancelled;
            this.consumerDispatcher = consumerDispatcher;
            this.eventBus = eventBus;
            this.handlerRunner = handlerRunner;
            Model = model;
        }

        public MessageHandler OnMessage { get; }
        public IQueue Queue { get; }
        public string ConsumerTag { get; private set; }

        /// <inheritdoc />
        public void HandleBasicConsumeOk(string consumerTag)
        {
            ConsumerTag = consumerTag;
        }

        /// <inheritdoc />
        public void HandleBasicDeliver(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            ReadOnlyMemory<byte> body
        )
        {
            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat("Message delivered to consumer {consumerTag} with deliveryTag {deliveryTag}", consumerTag, deliveryTag);
            }

            if (disposed)
            {
                // this message's consumer has stopped, so just return
                logger.InfoFormat(
                    "Consumer with consumerTag {consumerTag} on queue {queue} has stopped running. Ignoring message",
                    ConsumerTag,
                    Queue.Name
                );

                return;
            }

            var bodyBytes = body.ToArray();
            var messageReceivedInfo = new MessageReceivedInfo(consumerTag, deliveryTag, redelivered, exchange, routingKey, Queue.Name);
            var messageProperties = new MessageProperties(properties);
            var context = new ConsumerExecutionContext(OnMessage, messageReceivedInfo, messageProperties, bodyBytes);

            eventBus.Publish(new DeliveredMessageEvent(messageReceivedInfo, messageProperties, bodyBytes));
            handlerRunner.InvokeUserMessageHandlerAsync(context)
                .ContinueWith(async x =>
                    {
                        var ackStrategy = await x.ConfigureAwait(false);
                        consumerDispatcher.QueueAction(() =>
                        {
                            var ackResult = ackStrategy(Model, deliveryTag);
                            eventBus.Publish(new AckEvent(messageReceivedInfo, messageProperties, bodyBytes, ackResult));
                        });
                    },
                    TaskContinuationOptions.ExecuteSynchronously
                );
        }

        /// <summary>
        /// Cancel means that an external signal has requested that this consumer should
        /// be cancelled. This is _not_ the same as when an internal consumer stops consuming
        /// because it has lost its channel/connection.
        /// </summary>
        private void Cancel()
        {
            cancelled?.Invoke(this);
            ConsumerCancelled?.Invoke(this, new ConsumerEventArgs(new [] {ConsumerTag}));
        }

        /// <inheritdoc />
        public void HandleBasicCancelOk(string consumerTag)
        {
            Cancel();
        }

        /// <inheritdoc />
        public void HandleBasicCancel(string consumerTag)
        {
            Cancel();
            logger.InfoFormat(
                "Consumer with consumerTag {consumerTag} has cancelled",
                consumerTag
            );
        }

        /// <inheritdoc />
        public void HandleModelShutdown(object model, ShutdownEventArgs reason)
        {
            logger.InfoFormat(
                "Consumer with consumerTag {consumerTag} on queue {queue} has shutdown with reason {reason}",
                ConsumerTag,
                Queue.Name,
                reason
            );
        }

        /// <inheritdoc />
        public IModel Model { get; }

        /// <inheritdoc />
        public event EventHandler<ConsumerEventArgs> ConsumerCancelled;

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            eventBus.Publish(new ConsumerModelDisposedEvent(ConsumerTag));
        }
    }

    public class InternalConsumer : IInternalConsumer
    {
        private readonly IPersistentConnection connection;
        private readonly IConsumerDispatcher consumerDispatcher;
        private readonly IConventions conventions;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;
        private readonly ILog logger = LogProvider.For<InternalConsumer>();
        private ICollection<BasicConsumer> basicConsumers;

        private HashSet<BasicConsumer> cancelledConsumer;

        private bool disposed;

        private readonly object modelLock = new object();

        public InternalConsumer(
            IPersistentConnection connection,
            IHandlerRunner handlerRunner,
            IConsumerDispatcher consumerDispatcher,
            IConventions conventions,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");
            Preconditions.CheckNotNull(consumerDispatcher, "consumerDispatcher");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.connection = connection;
            this.handlerRunner = handlerRunner;
            this.consumerDispatcher = consumerDispatcher;
            this.conventions = conventions;
            this.eventBus = eventBus;
        }

        public IModel Model { get; private set; }

        /// <inheritdoc />
        public event Action<IInternalConsumer> Cancelled;

        /// <inheritdoc />
        public StartConsumingStatus StartConsuming(
            IReadOnlyCollection<Tuple<IQueue, MessageHandler>> queueConsumerPairs,
            ConsumerConfiguration configuration
        )
        {
            Preconditions.CheckNotNull(queueConsumerPairs, nameof(queueConsumerPairs));
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            try
            {
                InitModel(configuration.PrefetchCount, true);

                basicConsumers = new List<BasicConsumer>();

                foreach (var p in queueConsumerPairs)
                {
                    var queue = p.Item1;
                    var onMessage = p.Item2;
                    var consumerTag = conventions.ConsumerTagConvention();
                    try
                    {
                        var basicConsumer = new BasicConsumer(SingleBasicConsumerCancelled, consumerDispatcher, queue, eventBus, handlerRunner, onMessage, Model);

                        Model.BasicConsume(
                            queue.Name, // queue
                            false, // noAck
                            consumerTag, // consumerTag
                            true,
                            configuration.IsExclusive,
                            configuration.Arguments, // arguments
                            basicConsumer // consumer
                        );

                        basicConsumers.Add(basicConsumer);

                        logger.InfoFormat(
                            "Declared consumer with consumerTag {consumerTag} on queue={queue} and configuration {configuration}",
                            queue.Name,
                            consumerTag,
                            configuration
                        );
                    }
                    catch (Exception exception)
                    {
                        logger.Error(
                            exception,
                            "Consume with consumerTag {consumerTag} on queue {queue} failed",
                            queue.Name,
                            consumerTag
                        );
                        return StartConsumingStatus.Failed;
                    }
                }

                return StartConsumingStatus.Succeed;
            }
            catch (Exception exception)
            {
                logger.Error(
                    exception,
                    "Consume on queue {queue} failed",
                    string.Join(";", queueConsumerPairs.Select(x => x.Item1.Name))
                );
                return StartConsumingStatus.Failed;
            }
        }

        /// <inheritdoc />
        public StartConsumingStatus StartConsuming(
            IQueue queue,
            MessageHandler onMessage,
            ConsumerConfiguration configuration
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configuration, "configuration");

            var consumerTag = conventions.ConsumerTagConvention();
            try
            {
                InitModel(configuration.PrefetchCount, false);

                var basicConsumer = new BasicConsumer(SingleBasicConsumerCancelled, consumerDispatcher, queue, eventBus, handlerRunner, onMessage, Model);

                basicConsumers = new[] { basicConsumer };

                Model.BasicConsume(
                    queue.Name, // queue
                    false, // noAck
                    consumerTag, // consumerTag
                    true,
                    configuration.IsExclusive,
                    configuration.Arguments, // arguments
                    basicConsumer // consumer
                );

                logger.InfoFormat(
                    "Declared consumer with consumerTag {consumerTag} on queue {queue} and configuration {configuration}",
                    consumerTag,
                    queue.Name,
                    configuration
                );

                return StartConsumingStatus.Succeed;
            }
            catch (Exception exception)
            {
                logger.Error(
                    exception,
                    "Consume with consumerTag {consumerTag} from queue {queue} has failed",
                    consumerTag,
                    queue.Name
                );
                return StartConsumingStatus.Failed;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            var model = Model;
            if (model == null) return;

            // Queued because we may be on the RabbitMQ.Client dispatch thread.
            var disposedEvent = new AutoResetEvent(false);
            consumerDispatcher.QueueAction(() =>
            {
                try
                {
                    foreach (var c in basicConsumers)
                        c.Dispose();
                    model.Dispose();
                }
                finally
                {
                    disposedEvent.Set();
                }
            }, true);

            disposedEvent.WaitOne();
        }

        private void InitModel(ushort prefetchCount, bool globalQos)
        {
            if (Model == null)
            {
                lock (modelLock)
                {
                    if (Model == null)
                    {
                        Model = connection.CreateModel();
                        Model.BasicQos(0, prefetchCount, globalQos);
                    }
                }
            }
        }

        private void SingleBasicConsumerCancelled(BasicConsumer consumer)
        {
            if (cancelledConsumer == null)
                cancelledConsumer = new HashSet<BasicConsumer>();
            cancelledConsumer.Add(consumer);

            if (cancelledConsumer.Count == basicConsumers.Count)
            {
                cancelledConsumer = null;
                Cancelled?.Invoke(this);
            }
        }
    }
}
