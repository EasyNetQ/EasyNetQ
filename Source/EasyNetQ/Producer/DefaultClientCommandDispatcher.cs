using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;

namespace EasyNetQ.Producer
{
    /// <inheritdoc />
    public class DefaultClientCommandDispatcher : IClientCommandDispatcher
    {
        private readonly IPersistentChannel persistentChannel;
        private readonly AsyncLock persistentChannelLock = new AsyncLock();

        /// <summary>
        /// Creates DefaultClientCommandDispatcher
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="connection">The connection</param>
        /// <param name="persistentChannelFactory">The persistent channel factory</param>
        public DefaultClientCommandDispatcher(
            ConnectionConfiguration configuration,
            IPersistentConnection connection,
            IPersistentChannelFactory persistentChannelFactory
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            persistentChannel = persistentChannelFactory.CreatePersistentChannel(connection);
        }

        /// <inheritdoc />
        public async Task<T> InvokeAsync<T>(Func<IModel, T> channelAction, CancellationToken cancellationToken)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            using (await persistentChannelLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
                return await persistentChannel.InvokeChannelActionAsync(
                    channelAction, cancellationToken
                ).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            persistentChannelLock.Dispose();
            persistentChannel.Dispose();
        }
    }
}
