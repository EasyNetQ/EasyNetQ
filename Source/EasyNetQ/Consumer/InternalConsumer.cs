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
            var messsageProperties = new MessageProperties(properties);
            var context = new ConsumerExecutionContext(OnMessage, messageReceivedInfo, messsageProperties, body);

            eventBus.Publish(new DeliveredMessageEvent(messageReceivedInfo, messsageProperties, body));
            handlerRunner.InvokeUserMessageHandlerAsync(context)
                         .ContinueWith(async x =>
                            {
                                var ackStrategy = await x.ConfigureAwait(false);
                                consumerDispatcher.QueueAction(() =>
                                {
                                    var ackResult = ackStrategy(Model, deliveryTag);
                                    eventBus.Publish(new AckEvent(messageReceivedInfo, messsageProperties, body, ackResult));
                                });
                            },
                            TaskContinuationOptions.ExecuteSynchronously
                         );
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
        private readonly ILog logger = LogProvider.For<InternalConsumer>();
        
        private readonly IHandlerRunner handlerRunner;
        private readonly IConsumerDispatcher consumerDispatcher;
        private readonly IConventions conventions;
        private readonly IEventBus eventBus;
        private ICollection<BasicConsumer> basicConsumers;

        public IModel Model { get; private set; }

        public event Action<IInternalConsumer> Cancelled;

        public InternalConsumer(
            IHandlerRunner handlerRunner,
            IConsumerDispatcher consumerDispatcher,
            IConventions conventions,
            IEventBus eventBus)
        {
            Preconditions.CheckNotNull(handlerRunner, "handlerRunner");
            Preconditions.CheckNotNull(consumerDispatcher, "consumerDispatcher");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.handlerRunner = handlerRunner;
            this.consumerDispatcher = consumerDispatcher;
            this.conventions = conventions;
            this.eventBus = eventBus;
        }

        public StartConsumingStatus StartConsuming(IPersistentConnection connection, ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs, IConsumerConfiguration configuration)
        {
            Preconditions.CheckNotNull(connection, nameof(connection));
            Preconditions.CheckNotNull(queueConsumerPairs, nameof(queueConsumerPairs));
            Preconditions.CheckNotNull(configuration, nameof(configuration));


            IDictionary<string, object> arguments = new Dictionary<string, object>
                {
                    {"x-priority", configuration.Priority}
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
                        var basicConsumer = new BasicConsumer(SingleBasicConsumerCancelled, consumerDispatcher, queue, eventBus, handlerRunner, onMessage, Model);

                        Model.BasicConsume(
                            queue.Name, // queue
                            false, // noAck
                            consumerTag, // consumerTag
                            true,
                            configuration.IsExclusive,
                            arguments, // arguments
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
                    {"x-priority", configuration.Priority}
                };
            try
            {
                Model = connection.CreateModel();

                var basicConsumer = new BasicConsumer(SingleBasicConsumerCancelled, consumerDispatcher, queue, eventBus, handlerRunner, onMessage, Model);

                basicConsumers = new[] { basicConsumer };

                Model.BasicQos(0, configuration.PrefetchCount, false);

                Model.BasicConsume(
                    queue.Name,         // queue
                    false,              // noAck
                    consumerTag,        // consumerTag
                    true,
                    configuration.IsExclusive,
                    arguments,          // arguments
                    basicConsumer);     // consumer

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
                var disposedEvent = new AutoResetEvent(false);
                consumerDispatcher.QueueAction(() =>
                    {
                        Model.Dispose();
                        foreach (var c in basicConsumers)
                            c.Dispose();
                        disposedEvent.Set();
                    });
                disposedEvent.WaitOne();
            }
        }
    }
}