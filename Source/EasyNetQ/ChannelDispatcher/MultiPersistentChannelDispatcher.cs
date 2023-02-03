using System.Collections.Concurrent;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;

namespace EasyNetQ.ChannelDispatcher;

/// <summary>
///     Invokes client commands using multiple channels
/// </summary>
public sealed class MultiPersistentChannelDispatcher : IPersistentChannelDispatcher, IDisposable
{
    private readonly ConcurrentDictionary<PersistentChannelDispatchOptions, AsyncQueue<IPersistentChannel>> channelsPoolPerOptions;
    private readonly Func<PersistentChannelDispatchOptions, AsyncQueue<IPersistentChannel>> channelsPoolFactory;

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
        channelsPoolPerOptions = new ConcurrentDictionary<PersistentChannelDispatchOptions, AsyncQueue<IPersistentChannel>>();
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
                channel!.Dispose();
            x.Dispose();
        });
    }

    /// <inheritdoc />
    public async ValueTask<TResult> InvokeAsync<TResult, TChannelAction>(
        TChannelAction channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        var channelsPool = channelsPoolPerOptions.GetOrAdd(options, channelsPoolFactory);
        var channel = await channelsPool.DequeueAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await channel.InvokeChannelActionAsync<TResult, TChannelAction>(channelAction, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            channelsPool.Enqueue(channel);
        }
    }
}
