using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     Invokes client commands using single channel
    /// </summary>
    public sealed class SingleChannelClientCommandDispatcher : IClientCommandDispatcher
    {
        private readonly IPersistentChannel channel;
        private readonly AsyncLock channelLock = new AsyncLock();

        /// <summary>
        /// Creates DefaultClientCommandDispatcher
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="connection">The connection</param>
        /// <param name="channelFactory">The persistent channel factory</param>
        public SingleChannelClientCommandDispatcher(
            ConnectionConfiguration configuration,
            IPersistentConnection connection,
            IPersistentChannelFactory channelFactory
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(channelFactory, "channelFactory");

            channel = channelFactory.CreatePersistentChannel(connection);
        }

        /// <inheritdoc />
        public async Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            using var releaser = await channelLock.AcquireAsync(cancellationToken).ConfigureAwait(false);
            return await channel.InvokeChannelActionAsync(channelAction, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            channelLock.Dispose();
            channel.Dispose();
        }
    }
}
