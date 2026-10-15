using System.Collections.Concurrent;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;

namespace EasyNetQ.ChannelDispatcher;

/// <summary>
///     Invokes client commands using single channel
/// </summary>
public sealed class SinglePersistentChannelDispatcher : IPersistentChannelDispatcher
{
    private readonly ConcurrentDictionary<PersistentChannelDispatchOptions, Lazy<IPersistentChannel>> channelPerOptions;
    private readonly Func<PersistentChannelDispatchOptions, Lazy<IPersistentChannel>> createChannelFactory;

    /// <summary>
    /// Creates a dispatcher
    /// </summary>
    public SinglePersistentChannelDispatcher(
        IProducerConnection producerConnection,
        IConsumerConnection consumerConnection,
        IPersistentChannelFactory channelFactory
    )
    {
        channelPerOptions = new ConcurrentDictionary<PersistentChannelDispatchOptions, Lazy<IPersistentChannel>>();
        createChannelFactory = o => new Lazy<IPersistentChannel>(() =>
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
        });
    }

    /// <inheritdoc />
    public ValueTask<TResult> InvokeAsync<TResult, TChannelAction>(
        TChannelAction channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        // Lazy guarantees the factory runs once per options even when GetOrAdd races
        var channel = channelPerOptions.GetOrAdd(options, createChannelFactory).Value;
        return channel.InvokeChannelActionAsync<TResult, TChannelAction>(channelAction, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var item in channelPerOptions)
        {
            if (item.Value.IsValueCreated)
                await item.Value.Value.DisposeAsync();
        }
        channelPerOptions.Clear();
    }
}
