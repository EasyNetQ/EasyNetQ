using System;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    /// The result of an <see cref="IBus"/> Subscribe or SubscribeAsync operation.
    /// In order to cancel the subscription, call dispose on this object or on ConsumerCancellation.
    /// </summary>
    public interface ISubscriptionResult : IDisposable
    {
        /// <summary>
        /// The <see cref="IExchange"/> to which <see cref="Queue"/> is bound.
        /// </summary>
        IExchange Exchange { get; }

        /// <summary>
        /// The <see cref="IQueue"/> that the underlying <see cref="IConsumer"/> is consuming.
        /// </summary>
        IQueue Queue { get; }

        /// <summary>
        /// The <see cref="IConsumer"/> cancellation, which can be disposed to cancel the subscription.
        /// </summary>
        IDisposable ConsumerCancellation { get; }
    }
}
