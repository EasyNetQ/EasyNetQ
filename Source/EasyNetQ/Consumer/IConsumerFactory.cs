using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public interface IConsumerFactory : IDisposable
    {
        IConsumer CreateConsumer(
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task>>> queueConsumerPairs,
            IConsumerConfiguration configuration
        );

        IConsumer CreateConsumer(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage,
            IConsumerConfiguration configuration
        );
    }
}
