using System;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Logging;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer
{
    public class BasicConsumer : IBasicConsumer, IDisposable
    {
        private readonly ILog logger = LogProvider.For<BasicConsumer>();
        private readonly Action<BasicConsumer> cancelled;
        private readonly IConsumerDispatcher consumerDispatcher;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;

        public BasicConsumer(Action<BasicConsumer> cancelled, IConsumerDispatcher consumerDispatcher, IQueue queue, IEventBus eventBus, IHandlerRunner handlerRunner, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, IModel model)
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

        public Func<byte[], MessageProperties, MessageReceivedInfo, Task> OnMessage { get; }
        public IQueue Queue { get; }
        public string ConsumerTag { get; private set; }

        public void HandleBasicConsumeOk(string consumerTag)
        {
            ConsumerTag = consumerTag;
        }

        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
            IBasicProperties properties, ReadOnlyMemory<byte> body)
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
            // copy to temp variable to be thread safe.
            var localCancelled = cancelled;
            localCancelled?.Invoke(this);

            var consumerCancelled = ConsumerCancelled;
            consumerCancelled?.Invoke(this, new ConsumerEventArgs(new [] {ConsumerTag}));
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            Cancel();
        }

        public void HandleBasicCancel(string consumerTag)
        {
            Cancel();
            logger.InfoFormat(
                "Consumer with consumerTag {consumerTag} has cancelled",
                consumerTag
            );
        }

        public void HandleModelShutdown(object model, ShutdownEventArgs reason)
        {
            logger.InfoFormat(
                "Consumer with consumerTag {consumerTag} on queue {queue} has shutdown with reason {reason}",
                ConsumerTag,
                Queue.Name,
                reason
            );
            Cancel();
        }

        public IModel Model { get; }
        public event EventHandler<ConsumerEventArgs> ConsumerCancelled;

        private bool disposed;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            eventBus.Publish(new ConsumerModelDisposedEvent(ConsumerTag));
        }
    }
}
