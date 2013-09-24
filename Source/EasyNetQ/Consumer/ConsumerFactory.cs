using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class ConsumerFactory : IConsumerFactory
    {
        private readonly IInternalConsumerFactory internalConsumerFactory;

        private readonly ConcurrentDictionary<IConsumer, object> consumers = new ConcurrentDictionary<IConsumer, object>();

        public ConsumerFactory(IInternalConsumerFactory internalConsumerFactory)
        {
            Preconditions.CheckNotNull(internalConsumerFactory, "internalConsumerFactory");

            this.internalConsumerFactory = internalConsumerFactory;
        }

        public IConsumer CreateConsumer(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(connection, "connection");

            var consumer = CreateConsumerInstance(queue, onMessage, connection);

            consumer.RemoveMeFromList += theConsumer =>
                {
                    object value;
                    consumers.TryRemove(theConsumer, out value);
                };

            consumers.TryAdd(consumer, null);
            return consumer;
        }

        /// <summary>
        /// Create the correct implementation of IConsumer based on queue properties
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="onMessage"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        private IConsumer CreateConsumerInstance(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection)
        {
            if (queue.IsSingleUse)
            {
                return new SingleUseConsumer(queue, onMessage, connection, internalConsumerFactory);
            }

            if (queue.IsExclusive)
            {
                return new TransientConsumer(queue, onMessage, connection, internalConsumerFactory);
            }

            return new PersistentConsumer(queue, onMessage, connection, internalConsumerFactory);
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