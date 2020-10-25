using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Logging;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer
{
    /// <inheritdoc />
    public class Consumer : IConsumer
    {
        private readonly ILog logger = LogProvider.For<Consumer>();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly ConsumerConfiguration configuration;
        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;
        private readonly MessageHandler onMessage;
        private readonly IPersistentConnection connection;
        private readonly IQueue queue;

        private AsyncEventingBasicConsumer consumer;
        private IModel channel;

        /// <summary>
        ///     Creates Consumer
        /// </summary>
        public Consumer(
            IPersistentConnection connection,
            IQueue queue,
            MessageHandler onMessage,
            ConsumerConfiguration configuration,
            IEventBus eventBus,
            IHandlerRunner handlerRunner
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.connection = connection;
            this.queue = queue;
            this.onMessage = onMessage;
            this.configuration = configuration;
            this.eventBus = eventBus;
            this.handlerRunner = handlerRunner;
        }

        /// <inheritdoc />
        public void StartConsuming()
        {
            channel = connection.CreateModel();
            channel.BasicQos(0, configuration.PrefetchCount, false);
            consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += OnMessageReceived;
            channel.BasicConsume(
                queue.Name, false, configuration.ConsumerTag, false, false, configuration.Arguments, consumer
            );
            eventBus.Publish(new StartConsumingSucceededEvent(this, queue));
        }

        private async Task OnMessageReceived(object sender, BasicDeliverEventArgs @event)
        {
            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "Message delivered to consumer {consumerTag} with deliveryTag {deliveryTag}",
                    @event.ConsumerTag,
                    @event.DeliveryTag
                );
            }

            if (cancellation.IsCancellationRequested)
            {
                logger.InfoFormat(
                    "Consumer with consumerTag {consumerTag} on queue {queue} has stopped running. Ignoring message",
                    @event.ConsumerTag,
                    queue.Name
                );

                return;
            }

            var bodyBytes = @event.Body.ToArray();
            var receivedInfo = new MessageReceivedInfo(
                @event.ConsumerTag,
                @event.DeliveryTag,
                @event.Redelivered,
                @event.Exchange,
                @event.RoutingKey,
                queue.Name
            );
            var properties = new MessageProperties(@event.BasicProperties);
            eventBus.Publish(new DeliveredMessageEvent(receivedInfo, properties, bodyBytes));

            var context = new ConsumerExecutionContext(onMessage, receivedInfo, properties, bodyBytes);
            var ackStrategy = await handlerRunner.InvokeUserMessageHandlerAsync(
                context, cancellation.Token
            ).ConfigureAwait(false);

            var ackResult = ackStrategy(consumer.Model, @event.DeliveryTag);
            eventBus.Publish(new AckEvent(receivedInfo, properties, bodyBytes, ackResult));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            channel.BasicCancel(configuration.ConsumerTag);
            consumer.Received -= OnMessageReceived;
            cancellation.Cancel();
            channel.Dispose();
            cancellation.Dispose();
            eventBus.Publish(new StoppedConsumingEvent(this));
        }
    }
}
