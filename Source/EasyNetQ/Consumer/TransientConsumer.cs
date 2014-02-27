using System;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class TransientConsumer : IConsumer
    {
        private readonly IQueue queue;
        private readonly Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;
        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;
        private readonly Action onCancel;

        private IInternalConsumer internalConsumer;

        public TransientConsumer(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection, 
            IInternalConsumerFactory internalConsumerFactory, 
            IEventBus eventBus,
            Action onCancel
            )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(onCancel, "onShutdown");

            this.queue = queue;
            this.onMessage = onMessage;
            this.connection = connection;
            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;
            this.onCancel = onCancel;
        }

        public IDisposable StartConsuming()
        {
            internalConsumer = internalConsumerFactory.CreateConsumer();

            internalConsumer.Cancelled += consumer =>
            {
                onCancel();
                Dispose();
            };            

            internalConsumer.StartConsuming(
                connection,
                queue,
                onMessage);

            return new ConsumerCancellation(Dispose);
        }

        private bool disposed;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            eventBus.Publish(new StoppedConsumingEvent(this));
            if (internalConsumer != null)
            {
                internalConsumer.Dispose();
            }
        }
    }
}