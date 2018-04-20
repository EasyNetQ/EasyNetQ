using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer
{
    public interface IInternalConsumer : IDisposable
    {
        StartConsumingStatus StartConsuming(
            IPersistentConnection connection,
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IConsumerConfiguration configuration
            );

        StartConsumingStatus StartConsuming(
            IPersistentConnection connection,
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs,
            IConsumerConfiguration configuration
            );

        event Action<IInternalConsumer> Cancelled;
    }

    public class BasicConsumer : IBasicConsumer, IDisposable
    {
        private readonly Action<BasicConsumer> cancelled;
        private readonly IConsumerDispatcher consumerDispatcher;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;
        private readonly IEasyNetQLogger logger;
        public BasicConsumer(Action<BasicConsumer> cancelled, IConsumerDispatcher consumerDispatcher, IQueue queue, IEventBus eventBus, IHandlerRunner handlerRunner, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, IEasyNetQLogger logger, IModel model)
        {
            Queue = queue;
            OnMessage = onMessage;
            this.cancelled = cancelled;
            this.consumerDispatcher = consumerDispatcher;
            this.eventBus = eventBus;
            this.handlerRunner = handlerRunner;
            this.logger = logger;
            Model = model;
        }

        public Func<byte[], MessageProperties, MessageReceivedInfo, Task> OnMessage { get; }
        public IQueue Queue { get; }
        public string ConsumerTag { get; private set; }

        public void HandleBasicConsumeOk(string consumerTag)
        {
            ConsumerTag = consumerTag;
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

        public void HandleBasicCancelOk(string consumerTag)
        {
            Cancel();
        }

        public void HandleBasicCancel(string consumerTag)
        {
            Cancel();
            logger.InfoWrite("BasicCancel(Consumer Cancel Notification from broker) event received. " +
                             "Consumer tag: {0}", consumerTag);
        }

        public void HandleModelShutdown(object model, ShutdownEventArgs reason)
        {
            logger.InfoWrite("Consumer '{0}', consuming from queue '{1}', has shutdown. Reason: '{2}'",
                ConsumerTag, Queue.Name, reason.Cause);
        }
        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            logger.DebugWrite("HandleBasicDeliver on consumer: {0}, deliveryTag: {1}", consumerTag, deliveryTag);

            if (disposed)
            {
                // this message's consumer has stopped, so just return
                logger.InfoWrite("Consumer has stopped running. Consumer '{0}' on queue '{1}'. Ignoring message",
                    ConsumerTag, Queue.Name);
                return;
            }

            if (OnMessage == null)
            {
                logger.ErrorWrite("User consumer callback, 'onMessage' has not been set for consumer '{0}'." +
                    "Please call InternalConsumer.StartConsuming before passing the consumer to basic.consume",
                    ConsumerTag);
                return;
            }

            var messageReceivedInfo = new MessageReceivedInfo(consumerTag, deliveryTag, redelivered, exchange, routingKey, Queue.Name);
            var messsageProperties = new MessageProperties(properties);
            var context = new ConsumerExecutionContext(OnMessage, messageReceivedInfo, messsageProperties, body, this);

            consumerDispatcher.QueueAction(() =>
            {
                eventBus.Publish(new DeliveredMessageEvent(messageReceivedInfo, messsageProperties, body));
                handlerRunner.InvokeUserMessageHandler(context);
            });
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

    public class InternalConsumer : IInternalConsumer
    {
        private readonly IHandlerRunner handlerRunner;
        private readonly IEasyNetQLogger logger;
        private readonly IConsumerDispatcher consumerDispatcher;
        private readonly IConventions conventions;
        private readonly ConnectionConfiguration connectionConfiguration;
        private readonly IEventBus eventBus;

        private ICollection<BasicConsumer> basicConsumers;

        public IModel Model { get; private set; }
        public event EventHandler<ConsumerEventArgs> ConsumerCancelled;


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


        public StartConsumingStatus StartConsuming(IPersistentConnection connection, ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs, IConsumerConfiguration configuration)
        {
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(queueConsumerPairs, nameof(queueConsumerPairs));
            Preconditions.CheckNotNull(configuration, nameof(configuration));


            IDictionary<string, object> arguments = new Dictionary<string, object>
                {
                    {"x-priority", configuration.Priority},
                    {"x-cancel-on-ha-failover", configuration.CancelOnHaFailover || connectionConfiguration.CancelOnHaFailover}
                };
            try
            {
                Model = connection.CreateModel();

                Model.BasicQos(0, configuration.PrefetchCount, true);


                basicConsumers = new List<BasicConsumer>();

                foreach (var p in queueConsumerPairs)
                {
                    var queue = p.Item1;
                    var onMessage = p.Item2;
                    var consumerTag = conventions.ConsumerTagConvention();
                    try
                    {
                        var basicConsumer = new BasicConsumer(SingleBasicConsumerCancelled, consumerDispatcher, queue, eventBus, handlerRunner, onMessage, logger, Model);

                        Model.BasicConsume(
                            queue.Name, // queue
                            false, // noAck
                            consumerTag, // consumerTag
                            true,
                            configuration.IsExclusive,
                            arguments, // arguments
                            basicConsumer); // consumer

                        basicConsumers.Add(basicConsumer);

                        logger.InfoWrite("Declared Consumer. queue='{0}', consumer tag='{1}' prefetchcount={2} priority={3} x-cancel-on-ha-failover={4}",
                            queue.Name, consumerTag, configuration.PrefetchCount, configuration.Priority, configuration.CancelOnHaFailover);
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorWrite("Consume failed. queue='{0}', consumer tag='{1}', message='{2}'",
                            queue.Name, consumerTag, ex.Message);
                        return StartConsumingStatus.Failed;
                    }
                }
                
            }
            catch (Exception exception)
            {
                logger.ErrorWrite("Consume failed. queue='{0}', message='{1}'",
                    string.Join(";", queueConsumerPairs.Select(x => x.Item1.Name)), exception.Message);
                return StartConsumingStatus.Failed;
            }
            return StartConsumingStatus.Succeed;
        }

        public StartConsumingStatus StartConsuming(
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

            var consumerTag = conventions.ConsumerTagConvention();
            IDictionary<string, object> arguments = new Dictionary<string, object>
                {
                    {"x-priority", configuration.Priority},
                    {"x-cancel-on-ha-failover", configuration.CancelOnHaFailover || connectionConfiguration.CancelOnHaFailover}
                };
            try
            {
                Model = connection.CreateModel();

                var basicConsumer = new BasicConsumer(SingleBasicConsumerCancelled, consumerDispatcher, queue, eventBus, handlerRunner, onMessage, logger, Model);

                basicConsumers = new[] { basicConsumer };

                Model.BasicQos(0, configuration.PrefetchCount, false);

                Model.BasicConsume(
                    queue.Name,         // queue
                    false,              // noAck
                    consumerTag,        // consumerTag
                    true,
                    configuration.IsExclusive,
                    arguments,          // arguments
                    basicConsumer);              // consumer

                logger.InfoWrite("Declared Consumer. queue='{0}', consumer tag='{1}' prefetchcount={2} priority={3} x-cancel-on-ha-failover={4}",
                                  queue.Name, consumerTag, configuration.PrefetchCount, configuration.Priority, configuration.CancelOnHaFailover);
            }
            catch (Exception exception)
            {
                logger.ErrorWrite("Consume failed. queue='{0}', consumer tag='{1}', message='{2}'",
                                 queue.Name, consumerTag, exception.Message);
                return StartConsumingStatus.Failed;
            }
            return StartConsumingStatus.Succeed;
        }


        private HashSet<BasicConsumer> cancelledConsumer;
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
                        foreach (var c in basicConsumers)
                            c.Dispose();
                        
                    });
            }
        }
    }
}