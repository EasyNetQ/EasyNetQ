using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class PersistentConsumerFactory : IConsumerFactory
    {
        private readonly IInternalConsumerFactory internalConsumerFactory;

        private readonly IList<IConsumer> consumers = new List<IConsumer>();

        public PersistentConsumerFactory(IInternalConsumerFactory internalConsumerFactory)
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

            var consumer = new PersistentConsumer(
                queue, 
                onMessage,
                connection, 
                internalConsumerFactory);

            consumers.Add(consumer);
            return consumer;
        }

        public void Dispose()
        {
            foreach (var consumer in consumers)
            {
                consumer.Dispose();
            }
        }
    }
}