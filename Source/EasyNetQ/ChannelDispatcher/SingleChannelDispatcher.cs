using System;
using System.Collections.Concurrent;
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
    ///     Invokes client commands using single channel
    /// </summary>
    public sealed class SingleChannelDispatcher : IChannelDispatcher
    {
        private readonly ConcurrentDictionary<ChannelDispatchOptions, IPersistentChannel> channelPerOptions;
        private readonly Func<ChannelDispatchOptions, IPersistentChannel> createChannelFactory;

        /// <summary>
        /// Creates a dispatcher
        /// </summary>
        public SingleChannelDispatcher(IProducerConnection producerConnection, IConsumerConnection consumerConnection, IPersistentChannelFactory channelFactory)
        {
            Preconditions.CheckNotNull(producerConnection, nameof(producerConnection));
            Preconditions.CheckNotNull(consumerConnection, nameof(consumerConnection));
            Preconditions.CheckNotNull(channelFactory, nameof(channelFactory));

            channelPerOptions = new ConcurrentDictionary<ChannelDispatchOptions, IPersistentChannel>();
            createChannelFactory = o =>
            {
                var options = new PersistentChannelOptions(o.PublisherConfirms);
                return o.ConnectionType switch
                {
                    PersistentConnectionType.Producer => channelFactory.CreatePersistentChannel(
                        producerConnection, options
                    ),
                    PersistentConnectionType.Consumer => channelFactory.CreatePersistentChannel(
                        consumerConnection, options
                    ),
                    _ => throw new ArgumentOutOfRangeException()
                };
            };
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
