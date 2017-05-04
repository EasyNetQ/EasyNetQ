using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Topology;
using System.Linq;

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
            Preconditions.CheckNotNull(eventBus, "eventBus");

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
            IConsumerConfiguration configuration
            )
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");

            var consumer = CreateConsumerInstance(queue, onMessage, connection, configuration);
            consumers.TryAdd(consumer, null);
            return consumer;
        }

        /// <summary>
        /// Create the correct implementation of IConsumer based on queue properties
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="onMessage"></param>
        /// <param name="connection"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private IConsumer CreateConsumerInstance(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection,
            IConsumerConfiguration configuration)
        {
            if (queue.IsExclusive)
            {
                return new TransientConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
            }
            if(configuration.IsExclusive)
                return new ExclusiveConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
            return new PersistentConsumer(queue, onMessage, connection, configuration, internalConsumerFactory, eventBus);
        }

        public IConsumer CreateConsumer(
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs, 
            IPersistentConnection connection, 
            IConsumerConfiguration configuration)
        {
            if (configuration.IsExclusive || queueConsumerPairs.Any(x => x.Item1.IsExclusive))
                throw new NotSupportedException("Exclusive multiple consuming is not supported.");

            return new PersistentMultipleConsumer(queueConsumerPairs, connection, configuration, internalConsumerFactory, eventBus);

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