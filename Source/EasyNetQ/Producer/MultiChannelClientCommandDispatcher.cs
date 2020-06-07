using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<ChannelDispatchOptions, AsyncQueue<IPersistentChannel>> channelsPoolPerOptions;
        private readonly Func<ChannelDispatchOptions, AsyncQueue<IPersistentChannel>> channelsPoolFactory;

        /// <summary>
        /// Creates a dispatcher
        /// </summary>
        /// <param name="channelsCount">The max number of channels</param>
        /// <param name="channelFactory">The channel factory</param>
        public MultiChannelClientCommandDispatcher(int channelsCount, IPersistentChannelFactory channelFactory)
        {
            channelsPoolPerOptions = new ConcurrentDictionary<ChannelDispatchOptions, AsyncQueue<IPersistentChannel>>();
            channelsPoolFactory = o => new AsyncQueue<IPersistentChannel>(
                Enumerable.Range(0, channelsCount)
                    .Select(
                        _ => channelFactory.CreatePersistentChannel(new PersistentChannelOptions(o.PublisherConfirms))
                    )
            );
        }

        /// <inheritdoc />
        public void Dispose()
        {
            channelsPoolPerOptions.ClearAndDispose(x =>
            {
                while(x.TryDequeue(out var channel))
                    channel.Dispose();
                x.Dispose();
            });
        }

        /// <inheritdoc />
        public async Task<T> InvokeAsync<T>(
            Func<IModel, T> channelAction, ChannelDispatchOptions options, CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            // TODO channelsPoolFactory could be called multiple time, fix it
            var channelsPool = channelsPoolPerOptions.GetOrAdd(options, channelsPoolFactory);
            var channel = await channelsPool.DequeueAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await channel.InvokeChannelActionAsync(channelAction, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                channelsPool.Enqueue(channel);
            }
        }
    }
}
