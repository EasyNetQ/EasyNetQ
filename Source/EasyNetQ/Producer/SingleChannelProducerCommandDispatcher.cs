using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     Invokes client commands using single channel
    /// </summary>
    public sealed class SingleChannelProducerCommandDispatcher : IProducerCommandDispatcher
    {
        private readonly ConcurrentDictionary<ChannelDispatchOptions, IPersistentChannel> channelPerOptions;
        private readonly Func<ChannelDispatchOptions, IPersistentChannel> createChannelFactory;

        /// <summary>
        /// Creates a dispatcher
        /// </summary>
        public SingleChannelProducerCommandDispatcher(IProducerConnection connection, IPersistentChannelFactory channelFactory)
        {
            Preconditions.CheckNotNull(channelFactory, nameof(channelFactory));

            channelPerOptions = new ConcurrentDictionary<ChannelDispatchOptions, IPersistentChannel>();
            createChannelFactory = o => channelFactory.CreatePersistentChannel(connection, new PersistentChannelOptions(o.PublisherConfirms));
        }

        /// <inheritdoc />
        public Task<T> InvokeAsync<T>(
            Func<IModel, T> channelAction, ChannelDispatchOptions channelOptions, CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(channelAction, nameof(channelAction));

            // TODO createChannelFactory could be called multiple time, fix it
            var channel = channelPerOptions.GetOrAdd(channelOptions, createChannelFactory);
            return channel.InvokeChannelActionAsync(channelAction, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose() => channelPerOptions.ClearAndDispose();
    }
}
