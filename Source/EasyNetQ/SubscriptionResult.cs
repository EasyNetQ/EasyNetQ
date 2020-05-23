using System;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public sealed class SubscriptionResult : ISubscriptionResult
    {
        public IExchange Exchange { get; }
        public IQueue Queue { get; }
        public IDisposable ConsumerCancellation { get; }

        public SubscriptionResult(IExchange exchange, IQueue queue, IDisposable consumerCancellation)
        {
            Preconditions.CheckNotNull(exchange, "exchange");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(consumerCancellation, "consumerCancellation");

            Exchange = exchange;
            Queue = queue;
            ConsumerCancellation = consumerCancellation;
        }

        public void Dispose()
        {
            ConsumerCancellation.Dispose();
        }
    }
}
