using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     Invokes client commands using multiple channels
    /// </summary>
    public sealed class MultiChannelClientCommandDispatcher : IClientCommandDispatcher
    {
        private readonly AsyncQueue<IPersistentChannel> channelsPool;

        public MultiChannelClientCommandDispatcher(
            int channelsCount, IPersistentConnection connection, IPersistentChannelFactory channelFactory
        )
        {
            channelsPool = new AsyncQueue<IPersistentChannel>(
                Enumerable.Range(0, channelsCount).Select(_ => channelFactory.CreatePersistentChannel(connection))
            );
        }

        /// <inheritdoc />
        public void Dispose() => channelsPool.Dispose();

        /// <inheritdoc />
        public async Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken)
        {
            using var channel = await channelsPool.DequeueAsync(cancellationToken).ConfigureAwait(false);
            return await channel.InvokeChannelActionAsync(channelAction, cancellationToken).ConfigureAwait(false);
        }
    }
}
