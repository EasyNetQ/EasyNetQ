using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Logging;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ.Consumer
{
    internal class AsyncBasicConsumer : AsyncDefaultBasicConsumer, IDisposable
    {
        private readonly ILog logger = LogProvider.For<AsyncBasicConsumer>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly AsyncCountdownEvent onTheFlyMessages = new AsyncCountdownEvent();

        private readonly IEventBus eventBus;
        private readonly IHandlerRunner handlerRunner;
        private readonly MessageHandler messageHandler;
        private readonly Queue queue;

        private volatile bool disposed;

        public AsyncBasicConsumer(
            IModel model,
            Queue queue,
            IEventBus eventBus,
            IHandlerRunner handlerRunner,
            MessageHandler messageHandler
        ) : base(model)
        {
            this.queue = queue;
            this.eventBus = eventBus;
            this.handlerRunner = handlerRunner;
            this.messageHandler = messageHandler;
        }

        public Queue Queue => queue;

        /// <inheritdoc />
        public override async Task OnCancel(params string[] consumerTags)
        {
            await base.OnCancel(consumerTags).ConfigureAwait(false);
            logger.InfoFormat(
                "Consumer with consumerTags {consumerTags} has cancelled",
                string.Join(", ", consumerTags)
            );
        }

        public override async Task HandleBasicDeliver(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            ReadOnlyMemory<byte> body
        )
        {
            if (cts.IsCancellationRequested)
                return;

            if (logger.IsDebugEnabled())
            {
                logger.DebugFormat(
                    "Message delivered to consumer {consumerTag} with deliveryTag {deliveryTag}",
                    consumerTag,
                    deliveryTag
                );
            }

            onTheFlyMessages.Increment();
            try
            {
                var messageBody = body;
                var messageReceivedInfo = new MessageReceivedInfo(
                    consumerTag, deliveryTag, redelivered, exchange, routingKey, queue.Name
                );
                var messageProperties = new MessageProperties(properties);
                eventBus.Publish(new DeliveredMessageEvent(messageReceivedInfo, messageProperties, messageBody));

                var context = new ConsumerExecutionContext(
                    messageHandler, messageReceivedInfo, messageProperties, messageBody
                );
                var ackStrategy = await handlerRunner.InvokeUserMessageHandlerAsync(context, cts.Token)
                    .ConfigureAwait(false);
                var ackResult = ackStrategy(Model, deliveryTag);
                eventBus.Publish(new AckEvent(messageReceivedInfo, messageProperties, messageBody, ackResult));
            }
            finally
            {
                onTheFlyMessages.Decrement();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            cts.Cancel();
            onTheFlyMessages.Wait();
            cts.Dispose();
            onTheFlyMessages.Dispose();
            eventBus.Publish(new ConsumerModelDisposedEvent(ConsumerTags));
        }
    }
}
