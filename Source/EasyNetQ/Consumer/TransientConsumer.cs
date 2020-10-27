using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class TransientConsumer : IConsumer
    {
        private readonly ConsumerConfiguration configuration;
        private readonly IEventBus eventBus;
        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly MessageHandler onMessage;
        private readonly IQueue queue;

        private bool disposed;

        private IInternalConsumer internalConsumer;

        public TransientConsumer(
            IQueue queue,
            MessageHandler onMessage,
            ConsumerConfiguration configuration,
            IInternalConsumerFactory internalConsumerFactory,
            IEventBus eventBus
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.queue = queue;
            this.onMessage = onMessage;
            this.configuration = configuration;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
        }

        /// <inheritdoc />
        public void StartConsuming()
        {
            internalConsumer = internalConsumerFactory.CreateConsumer();

            internalConsumer.Cancelled += consumer => Dispose();

            var status = internalConsumer.StartConsuming(
                queue,
                onMessage,
                configuration
            );

            if (status == StartConsumingStatus.Succeed)
                eventBus.Publish(new StartConsumingSucceededEvent(this, queue));
            else
                eventBus.Publish(new StartConsumingFailedEvent(this, queue));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            eventBus.Publish(new StoppedConsumingEvent(this));

            internalConsumer?.Dispose();
        }
    }
}
