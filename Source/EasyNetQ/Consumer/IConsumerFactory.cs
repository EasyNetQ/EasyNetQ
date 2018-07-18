using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public interface IConsumerFactory : IDisposable
    {
        IConsumer CreateConsumer(
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, Task>>> queueConsumerPairs,
            IPersistentConnection connection,
            IConsumerConfiguration configuration);

        IConsumer CreateConsumer(
            IQueue queue, 
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, 
            IPersistentConnection connection,
            IConsumerConfiguration configuration
            );
    }
}