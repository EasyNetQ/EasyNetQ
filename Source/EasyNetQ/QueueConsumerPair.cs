using System;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class QueueConsumerPair
    {
        public static QueueConsumerPair Create<T>(IQueue queue, Action<IMessage<T>, MessageReceivedInfo> onMessage) where T : class
        {
            Preconditions.CheckNotNull(queue, nameof(queue));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return Create<T>(queue, (message, info) => TaskHelpers.ExecuteSynchronously(() => onMessage(message, info)));
        }

        public static QueueConsumerPair Create<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage) where T : class
        {
            Preconditions.CheckNotNull(queue, nameof(queue));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return new QueueConsumerPair(queue, null, h => h.Add(onMessage));
        }

        public static QueueConsumerPair Consume(IQueue queue, Action<byte[], MessageProperties, MessageReceivedInfo> onMessage)
        {
            Preconditions.CheckNotNull(queue, nameof(queue));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return new QueueConsumerPair(queue, (bytes, properties, info) => TaskHelpers.ExecuteSynchronously(() => onMessage(bytes, properties, info)), null);
        }

        public static QueueConsumerPair Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            Preconditions.CheckNotNull(queue, nameof(queue));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return new QueueConsumerPair(queue, onMessage, null);
        }

        private QueueConsumerPair(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, Action<IHandlerRegistration> addHandlers)
        {
            Queue = queue;
            OnMessage = onMessage;
            AddHandlers = addHandlers;
        }

        public IQueue Queue { get; }
        public Func<byte[], MessageProperties, MessageReceivedInfo, Task> OnMessage { get; }
        public Action<IHandlerRegistration> AddHandlers { get; }

    }
}
