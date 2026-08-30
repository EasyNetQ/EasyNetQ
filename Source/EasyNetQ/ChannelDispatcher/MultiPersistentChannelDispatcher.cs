using System.Collections.Concurrent;
using System.Threading.Channels;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;

namespace EasyNetQ.ChannelDispatcher;

/// <summary>
///     Invokes client commands using multiple channels
/// </summary>
public sealed class MultiPersistentChannelDispatcher : IPersistentChannelDispatcher
{
    private readonly ConcurrentDictionary<PersistentChannelDispatchOptions, Channel<IPersistentChannel>> channelsPoolPerOptions;
    private readonly Func<PersistentChannelDispatchOptions, Channel<IPersistentChannel>> channelsPoolFactory;

    /// <summary>
    ///     Creates a dispatcher
    /// </summary>
    public MultiPersistentChannelDispatcher(
        int channelsCount,
        IProducerConnection producerConnection,
        IConsumerConnection consumerConnection,
        IPersistentChannelFactory channelFactory
    )
    {
        channelsPoolPerOptions = new ConcurrentDictionary<PersistentChannelDispatchOptions, Channel<IPersistentChannel>>();
        channelsPoolFactory = o =>
        {
            var options = new PersistentChannelOptions(o.PublisherConfirms);
            var pool = Channel.CreateUnbounded<IPersistentChannel>();
            for (var i = 0; i < channelsCount; i++)
            {
                pool.Writer.TryWrite(
                    o.ConnectionType switch
                    {
                        PersistentConnectionType.Producer => channelFactory.CreatePersistentChannel(producerConnection, options),
                        PersistentConnectionType.Consumer => channelFactory.CreatePersistentChannel(consumerConnection, options),
                        _ => throw new ArgumentOutOfRangeException()
                    }
                );
            }
            return pool;
        };
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var item in channelsPoolPerOptions)
        {
            item.Value.Writer.TryComplete();
            while (item.Value.Reader.TryRead(out var channel))
                await channel.DisposeAsync();
        }
        channelsPoolPerOptions.Clear();
    }

    /// <inheritdoc />
    public async ValueTask<TResult> InvokeAsync<TResult, TChannelAction>(
        TChannelAction channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        var channelsPool = channelsPoolPerOptions.GetOrAdd(options, channelsPoolFactory);
        var channel = await channelsPool.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await channel.InvokeChannelActionAsync<TResult, TChannelAction>(channelAction, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            channelsPool.Writer.TryWrite(channel);
        }
    }
}
