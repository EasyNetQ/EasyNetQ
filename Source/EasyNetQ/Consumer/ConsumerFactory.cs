﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class ConsumerFactory : IConsumerFactory
    {
        private readonly IInternalConsumerFactory internalConsumerFactory;
        private readonly IEventBus eventBus;

        private readonly ConcurrentDictionary<IConsumer, object> consumers = new ConcurrentDictionary<IConsumer, object>();

        public ConsumerFactory(IInternalConsumerFactory internalConsumerFactory, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");

            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;

            eventBus.Subscribe<StoppedConsumingEvent>(stoppedConsumingEvent =>
                {
                    object value;
                    consumers.TryRemove(stoppedConsumingEvent.Consumer, out value);
                });
        }

        public IConsumer CreateConsumer(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IPersistentConnection connection,
            Action onShutdown
            )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");

            var consumer = CreateConsumerInstance(queue, onMessage, connection, onShutdown);
            consumers.TryAdd(consumer, null);
            return consumer;
        }

        /// <summary>
        /// Create the correct implementation of IConsumer based on queue properties
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="onMessage"></param>
        /// <param name="connection"></param>
        /// <param name="onShutdown"></param>
        /// <returns></returns>
        private IConsumer CreateConsumerInstance(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage,
            IPersistentConnection connection,
            Action onShutdown
            )
        {
            if (queue.IsExclusive)
            {
                return new TransientConsumer(queue, onMessage, connection, internalConsumerFactory, eventBus, onShutdown);
            }

            return new PersistentConsumer(queue, onMessage, connection, internalConsumerFactory, eventBus, onShutdown);
        }

        public void Dispose()
        {
            foreach (var consumer in consumers.Keys)
            {
                consumer.Dispose();
            }
            internalConsumerFactory.Dispose();
        }
    }
}