﻿using System;
using System.Threading;
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

            return new QueueConsumerPair(queue, null, h => h.Add(onMessage));
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

            var onMessageAsync = TaskHelpers.FromAction<byte[], MessageProperties, MessageReceivedInfo>((m, p, i, c) => onMessage(m, p, i));
            return new QueueConsumerPair(queue, onMessageAsync, null);
        }

        public static QueueConsumerPair Consume(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage)
        {
            Preconditions.CheckNotNull(queue, nameof(queue));
            Preconditions.CheckNotNull(onMessage, nameof(onMessage));

            return new QueueConsumerPair(queue, (m, p, i, c) => onMessage(m, p, i), null);
        }

        private QueueConsumerPair(IQueue queue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage, Action<IHandlerRegistration> addHandlers)
        {
            Queue = queue;
            OnMessage = onMessage;
            AddHandlers = addHandlers;
        }

        public IQueue Queue { get; }
        public Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> OnMessage { get; }
        public Action<IHandlerRegistration> AddHandlers { get; }
    }
}
