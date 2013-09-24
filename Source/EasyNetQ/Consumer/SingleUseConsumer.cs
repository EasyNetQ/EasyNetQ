using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class SingleUseConsumer : IConsumer
    {
        private readonly IQueue queue;
        private readonly Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage;
        private readonly IPersistentConnection connection;
        private readonly IInternalConsumerFactory internalConsumerFactory;

        private IInternalConsumer internalConsumer;

        public event Action<IConsumer> RemoveMeFromList;

        public SingleUseConsumer(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection, 
            IInternalConsumerFactory internalConsumerFactory)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");

            this.queue = queue;
            this.onMessage = onMessage;
            this.connection = connection;
            this.internalConsumerFactory = internalConsumerFactory;
        }

        public void StartConsuming()
        {
            internalConsumer = internalConsumerFactory.CreateConsumer();

            internalConsumer.Cancelled += consumer => OnRemoveMeFromList();

            internalConsumer.AckOrNackWasSent += context =>
                {
                    OnRemoveMeFromList();
                    Dispose();
                };

            internalConsumer.StartConsuming(
                connection,
                queue,
                onMessage);

        }

        private void OnRemoveMeFromList()
        {
            var removeMeFromList = RemoveMeFromList;
            if (removeMeFromList != null)
            {
                removeMeFromList(this);
            }
        }

        public void Dispose()
        {
            if (internalConsumer != null)
            {
                internalConsumer.Dispose();
            }
        }
    }
}