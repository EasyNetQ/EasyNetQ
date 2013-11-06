using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ.Consumer
{
    public interface IInternalConsumer : IDisposable
    {
        void StartConsuming(
            IPersistentConnection connection,
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage);

        event Action<IInternalConsumer> Cancelled;
    }

    public class InternalConsumer : IBasicConsumer, IInternalConsumer
    {
        private readonly IHandlerRunner handlerRunner;
        private readonly IEasyNetQLogger logger;
        private readonly IConsumerDispatcher consumerDispatcher;
        private readonly IConventions conventions;
        private readonly IConnectionConfiguration connectionConfiguration;

        private Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private IQueue queue;

        public IModel Model { get; private set; }
        public string ConsumerTag { get; private set; }

        public event Action<IInternalConsumer> Cancelled;

        public InternalConsumer(
            IHandlerRunner handlerRunner, 
            IEasyNetQLogger logger, 
            IConsumerDispatcher consumerDispatcher, 
            IConventions conventions, 
            IConnectionConfiguration connectionConfiguration)
        {
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(consumerDispatcher, "consumerDispatcher");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");

            this.handlerRunner = handlerRunner;
            this.logger = logger;
            this.consumerDispatcher = consumerDispatcher;
            this.conventions = conventions;
            this.connectionConfiguration = connectionConfiguration;
        }

        public void StartConsuming(
            IPersistentConnection connection,
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");

            this.queue = queue;
            this.onMessage = onMessage;
            var consumerTag = conventions.ConsumerTagConvention();

            try
            {
                Model = connection.CreateModel();

                Model.BasicQos(0, connectionConfiguration.PrefetchCount, false);

                Model.BasicConsume(
                    queue.Name,         // queue
                    false,              // noAck 
                    consumerTag,        // consumerTag
                    this);              // consumer

                logger.InfoWrite("Declared Consumer. queue='{0}', consumer tag='{1}' prefetchcount={2}",
                                  queue.Name, consumerTag, connectionConfiguration.PrefetchCount);
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

            var messageRecievedInfo = new MessageReceivedInfo(consumerTag, deliveryTag, redelivered, exchange, routingKey, queue.Name);
            var messsageProperties = new MessageProperties(properties);
            var context = new ConsumerExecutionContext(onMessage, messageRecievedInfo, messsageProperties, body, this);

            consumerDispatcher.QueueAction(() => handlerRunner.InvokeUserMessageHandler(context));
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
                consumerDispatcher.QueueAction(() => Model.Dispose());
            }
        }
    }
}