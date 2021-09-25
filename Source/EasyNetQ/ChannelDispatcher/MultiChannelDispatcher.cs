using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using RabbitMQ.Client;

namespace EasyNetQ.ChannelDispatcher
{
    /// <summary>
    ///     Invokes client commands using multiple channels
    /// </summary>
    public sealed class MultiChannelDispatcher : IChannelDispatcher
    {
        private readonly ConcurrentDictionary<ChannelDispatchOptions, AsyncQueue<IPersistentChannel>> channelsPoolPerOptions;
        private readonly Func<ChannelDispatchOptions, AsyncQueue<IPersistentChannel>> channelsPoolFactory;

        /// <summary>
        ///     Creates a dispatcher
        /// </summary>
        public MultiChannelDispatcher(
            int channelsCount,
            IProducerConnection producerConnection,
            IConsumerConnection consumerConnection,
            IPersistentChannelFactory channelFactory
        )
        {
            channelsPoolPerOptions = new ConcurrentDictionary<ChannelDispatchOptions, AsyncQueue<IPersistentChannel>>();
            channelsPoolFactory = o =>
            {
                var options = new PersistentChannelOptions(o.PublisherConfirms);
                return new AsyncQueue<IPersistentChannel>(
                    Enumerable.Range(0, channelsCount)
                        .Select(
                            _ => o.ConnectionType switch
                            {
                                PersistentConnectionType.Producer => channelFactory.CreatePersistentChannel(
                                    producerConnection, options
                                ),
                                PersistentConnectionType.Consumer => channelFactory.CreatePersistentChannel(
                                    consumerConnection, options
                                ),
                                _ => throw new ArgumentOutOfRangeException()
                            }
                        )
                );
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            channelsPoolPerOptions.ClearAndDispose(x =>
            {
                while (x.TryDequeue(out var channel))
                    channel.Dispose();
                x.Dispose();
            });
        }

        /// <inheritdoc />
        public async Task<T> InvokeAsync<T>(
            Func<IModel, T> channelAction, ChannelDispatchOptions options, CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(channelAction, nameof(channelAction));

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
