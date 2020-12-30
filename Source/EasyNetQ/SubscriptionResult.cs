using System;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public sealed class SubscriptionResult : ISubscriptionResult
    {
        public Exchange Exchange { get; }
        public Queue Queue { get; }
        public IDisposable ConsumerCancellation { get; }

        public SubscriptionResult(Exchange exchange, Queue queue, IDisposable consumerCancellation)
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
