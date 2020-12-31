using System;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    /// The result of an <see cref="IBus"/> Subscribe or SubscribeAsync operation.
    /// In order to cancel the subscription, call dispose on this object or on ConsumerCancellation.
    /// </summary>
    public readonly struct SubscriptionResult : IDisposable
    {
        /// <summary>
        /// The <see cref="IConsumer"/> cancellation, which can be disposed to cancel the subscription.
        /// </summary>
        public SubscriptionResult(Exchange exchange, Queue queue, IDisposable consumerCancellation)
        {
            Preconditions.CheckNotNull(consumerCancellation, "consumerCancellation");

            Exchange = exchange;
            Queue = queue;
            ConsumerCancellation = consumerCancellation;
        }

        /// <summary>
        /// The <see cref="Exchange"/> to which <see cref="Queue"/> is bound.
        /// </summary>
        public Exchange Exchange { get; }

        /// <summary>
        /// The <see cref="Queue"/> that the underlying <see cref="IConsumer"/> is consuming.
        /// </summary>
        public Queue Queue { get; }

        /// <summary>
        /// The <see cref="IConsumer"/> cancellation, which can be disposed to cancel the subscription.
        /// </summary>
        private IDisposable ConsumerCancellation { get; }

        /// <inheritdoc />
        public void Dispose() => ConsumerCancellation.Dispose();
    }
}
