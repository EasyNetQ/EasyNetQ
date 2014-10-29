using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer
{
    public interface IInternalConsumer : IDisposable
    {
        void StartConsuming(
            IPersistentConnection connection,
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IConsumerConfiguration configuration
            );

        event Action<IInternalConsumer> Cancelled;
    }

    public class InternalConsumer : IBasicConsumer, IInternalConsumer
    {
        private readonly IHandlerRunner handlerRunner;
        private readonly IEasyNetQLogger logger;
        private readonly IConsumerDispatcher consumerDispatcher;
        private readonly IConventions conventions;
        private readonly ConnectionConfiguration connectionConfiguration;
        private readonly IEventBus eventBus;

        private Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private IQueue queue;

        public IModel Model { get; private set; }
        public event ConsumerCancelledEventHandler ConsumerCancelled;
        public string ConsumerTag { get; private set; }

        public event Action<IInternalConsumer> Cancelled;

        public InternalConsumer(
            IHandlerRunner handlerRunner,
            IEasyNetQLogger logger,
            IConsumerDispatcher consumerDispatcher,
            IConventions conventions,
            ConnectionConfiguration connectionConfiguration,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(consumerDispatcher, "consumerDispatcher");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.handlerRunner = handlerRunner;
            this.logger = logger;
            this.consumerDispatcher = consumerDispatcher;
            this.conventions = conventions;
            this.connectionConfiguration = connectionConfiguration;
            this.eventBus = eventBus;
        }

        public void StartConsuming(
            IPersistentConnection connection,
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IConsumerConfiguration configuration
            )
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.queue = queue;
            this.onMessage = onMessage;
            var consumerTag = conventions.ConsumerTagConvention();
            IDictionary<string, object> arguments = new Dictionary<string, object>
                {
                    {"x-priority", configuration.Priority},
                    {"x-cancel-on-ha-failover", configuration.CancelOnHaFailover || connectionConfiguration.CancelOnHaFailover}
                };
            try
            {
                Model = connection.CreateModel();

                Model.BasicQos(0, configuration.PrefetchCount, false);

                Model.BasicConsume(
                    queue.Name,         // queue
                    false,              // noAck
                    consumerTag,        // consumerTag
                    arguments,          // arguments
                    this);              // consumer

                logger.InfoWrite("Declared Consumer. queue='{0}', consumer tag='{1}' prefetchcount={2} priority={3} x-cancel-on-ha-failover={4}",
                                  queue.Name, consumerTag, connectionConfiguration.PrefetchCount, configuration.Priority, configuration.CancelOnHaFailover);
            }
            catch (Exception exception)
            {
                logger.InfoWrite("Consume failed. queue='{0}', consumer tag='{1}', message='{2}'",
                                 queue.Name, consumerTag, exception.Message);
            }
        }

        /// <summary>
        /// Cancel means that an external signal has requested that this consumer should
        /// be cancelled. This is _not_ the same as when an internal consumer stops consuming
        /// because it has lost its channel/connection.
        /// </summary>
        private void Cancel()
        {
            // copy to temp variable to be thread safe.
            var cancelled = Cancelled;
            if(cancelled != null) cancelled(this);

            var consumerCancelled = ConsumerCancelled;
            if(consumerCancelled != null) consumerCancelled(this, new ConsumerEventArgs(ConsumerTag));
        }

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
            Cancel();
            logger.InfoWrite("BasicCancel(Consumer Cancel Notification from broker) event received. " +
                             "Consumer tag: " + consumerTag);
        }

        public void HandleModelShutdown(IModel model, ShutdownEventArgs reason)
        {
            logger.InfoWrite("Consumer '{0}', consuming from queue '{1}', has shutdown. Reason: '{2}'",
                             ConsumerTag, queue.Name, reason.Cause);
        }

        public void HandleBasicDeliver(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            byte[] body)
        {
            logger.DebugWrite("HandleBasicDeliver on consumer: {0}, deliveryTag: {1}", consumerTag, deliveryTag);

            if (disposed)
            {
                // this message's consumer has stopped, so just return
                logger.InfoWrite("Consumer has stopped running. Consumer '{0}' on queue '{1}'. Ignoring message",
                    ConsumerTag, queue.Name);
                return;
            }

            if (onMessage == null)
            {
                logger.ErrorWrite("User consumer callback, 'onMessage' has not been set for consumer '{0}'." +
                    "Please call InternalConsumer.StartConsuming before passing the consumer to basic.consume",
                    ConsumerTag);
                return;
            }

            var messageReceivedInfo = new MessageReceivedInfo(consumerTag, deliveryTag, redelivered, exchange, routingKey, queue.Name);
            var messsageProperties = new MessageProperties(properties);
            var context = new ConsumerExecutionContext(onMessage, messageReceivedInfo, messsageProperties, body, this);

            consumerDispatcher.QueueAction(() =>
                {
                    eventBus.Publish(new DeliveredMessageEvent(messageReceivedInfo, messsageProperties, body));
                    handlerRunner.InvokeUserMessageHandler(context);
                });
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            var model = Model;
            if (model != null)
            {
                // Queued because we may be on the RabbitMQ.Client dispatch thread.
                consumerDispatcher.QueueAction(() =>
                    {
                        Model.Dispose();
                        eventBus.Publish(new ConsumerModelDisposedEvent(ConsumerTag));
                    });
            }
        }
    }
}