using System;
using System.Threading;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// An abstraction on top of channel which manages its persistence and invokes an action on it
    /// </summary>
    public interface IPersistentChannel : IDisposable
    {
        /// <summary>
        /// Invoke an action on channel
        /// </summary>
        /// <param name="channelAction">The action to invoke</param>
        /// <param name="cancellationToken">The cancellation token</param>
        void InvokeChannelAction(Action<IModel> channelAction, CancellationToken cancellationToken);
    }
}
