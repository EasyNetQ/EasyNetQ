using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Logging;
using EasyNetQ.Topology;
using RabbitMQ.Client;

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
                consumerDispatcher.QueueAction(() =>
                {
                    foreach (var c in basicConsumers)
                        c.Dispose();
                    model.Dispose();
                }, true);
            }
        }
    }
}
