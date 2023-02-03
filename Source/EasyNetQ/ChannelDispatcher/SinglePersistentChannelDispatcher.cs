using System.Collections.Concurrent;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;

namespace EasyNetQ.ChannelDispatcher;

/// <summary>
///     Invokes client commands using single channel
/// </summary>
public sealed class SinglePersistentChannelDispatcher : IPersistentChannelDispatcher, IDisposable
{
    private readonly ConcurrentDictionary<PersistentChannelDispatchOptions, IPersistentChannel> channelPerOptions;
    private readonly Func<PersistentChannelDispatchOptions, IPersistentChannel> createChannelFactory;

    /// <summary>
    /// Creates a dispatcher
    /// </summary>
    public SinglePersistentChannelDispatcher(
        IProducerConnection producerConnection,
        IConsumerConnection consumerConnection,
        IPersistentChannelFactory channelFactory
    )
    {
        channelPerOptions = new ConcurrentDictionary<PersistentChannelDispatchOptions, IPersistentChannel>();
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
    public ValueTask<TResult> InvokeAsync<TResult, TChannelAction>(
        TChannelAction channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        // TODO createChannelFactory could be called multiple time, fix it
        var channel = channelPerOptions.GetOrAdd(options, createChannelFactory);
        return channel.InvokeChannelActionAsync<TResult, TChannelAction>(channelAction, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose() => channelPerOptions.ClearAndDispose();
}
