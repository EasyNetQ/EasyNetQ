using System;
using EasyNetQ.Topology;

namespace EasyNetQ.Consumer
{
    public class SubscriptionResult : ISubscriptionResult
    {
        public IExchange Exchange { get; private set; }
        public IQueue Queue { get; private set; }
        public IDisposable ConsumerCancellation { get; private set; }

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
            if (ConsumerCancellation != null)
            {
                ConsumerCancellation.Dispose();
            }
        }
    }
}