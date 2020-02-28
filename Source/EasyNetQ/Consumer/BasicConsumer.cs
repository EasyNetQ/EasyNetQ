using System;
using System.Threading;
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
            Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage,
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

        public Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> OnMessage { get; }
        public IQueue Queue { get; }
        public string ConsumerTag { get; private set; }

        public void HandleBasicConsumeOk(string consumerTag)
        {
            ConsumerTag = consumerTag;
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            Cancel();
        }

        public void HandleBasicCancel(string consumerTag)
        {
            logger.InfoFormat(
                "Consumer with consumerTag {consumerTag} has cancelled",
                consumerTag
            );
            Cancel();
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

        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
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

            var messageReceivedInfo = new MessageReceivedInfo(consumerTag, deliveryTag, redelivered, exchange, routingKey, Queue.Name);
            var messageProperties = new MessageProperties(properties);
            var context = new ConsumerExecutionContext(OnMessage, messageReceivedInfo, messageProperties, body);

            eventBus.Publish(new DeliveredMessageEvent(messageReceivedInfo, messageProperties, body));
            handlerRunner.InvokeUserMessageHandlerAsync(context)
                .ContinueWith(async x =>
                    {
                        var ackStrategy = await x.ConfigureAwait(false);
                        consumerDispatcher.QueueAction(() =>
                        {
                            var ackResult = ackStrategy(Model, deliveryTag);
                            eventBus.Publish(new AckEvent(messageReceivedInfo, messageProperties, body, ackResult));
                        });
                    },
                    TaskContinuationOptions.ExecuteSynchronously
                );
        }

        public IModel Model { get; }
        public event EventHandler<ConsumerEventArgs> ConsumerCancelled;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            eventBus.Publish(new ConsumerModelDisposedEvent(ConsumerTag));
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
            consumerCancelled?.Invoke(this, new ConsumerEventArgs(ConsumerTag));
        }
    }
}
