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
            IReadOnlyCollection<Tuple<IQueue, MessageHandler>> queueConsumerPairs,
            IConsumerConfiguration configuration
        );

        IConsumer CreateConsumer(
            IQueue queue,
            MessageHandler onMessage,
            IConsumerConfiguration configuration
        );
    }
}
