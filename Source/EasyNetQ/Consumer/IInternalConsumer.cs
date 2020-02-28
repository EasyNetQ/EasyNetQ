using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public interface IInternalConsumer : IDisposable
    {
        StartConsumingStatus StartConsuming(
            IQueue queue,
            Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task> onMessage,
            IConsumerConfiguration configuration
        );

        StartConsumingStatus StartConsuming(
            ICollection<Tuple<IQueue, Func<byte[], MessageProperties, MessageReceivedInfo, CancellationToken, Task>>> queueConsumerPairs,
            IConsumerConfiguration configuration
        );

        event Action<IInternalConsumer> Cancelled;
    }
}
