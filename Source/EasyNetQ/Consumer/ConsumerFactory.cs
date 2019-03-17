﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class ConsumerFactory : IConsumerFactory
    {
        private readonly ConcurrentSet<IConsumer> consumers = new ConcurrentSet<IConsumer>();
        private readonly IEventBus eventBus;
        private readonly IInternalConsumerFactory internalConsumerFactory;

        public ConsumerFactory(IInternalConsumerFactory internalConsumerFactory, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.internalConsumerFactory = internalConsumerFactory;
            this.eventBus = eventBus;

            eventBus.Subscribe<StoppedConsumingEvent>(stoppedConsumingEvent => consumers.Remove(stoppedConsumingEvent.Consumer));
        }

        public IConsumer CreateConsumer(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage,
            IPersistentConnection connection,
            IConsumerConfiguration configuration
        )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");

            var consumer = CreateConsumerInstance(queue, onMessage, connection, configuration);
            consumers.Add(consumer);
            return consumer;
        }

        public IConsumer CreateConsumer(
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task>>> queueConsumerPairs,
            IPersistentConnection connection,
            IConsumerConfiguration configuration
        )
        {
            if (configuration.IsExclusive || queueConsumerPairs.Any(x => x.Item1.IsExclusive))
                throw new NotSupportedException("Exclusive multiple consuming is not supported.");

            return new PersistentMultipleConsumer(queueConsumerPairs, connection, configuration, internalConsumerFactory, eventBus);
        }


        public void Dispose()
        {
            foreach (var consumer in consumers) consumer.Dispose();
            internalConsumerFactory.Dispose();
        }

        /// <summary>
        ///     Create the correct implementation of IConsumer based on queue properties
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="onMessage"></param>
        /// <param name="connection"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private IConsumer CreateConsumerInstance(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage,
            IPersistentConnection connection,
            IConsumerConfiguration configuration
        )
        {
            if (queue.IsExclusive) return new TransientConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
            if (configuration.IsExclusive)
                return new ExclusiveConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
            return new PersistentConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
        }
    }
}
