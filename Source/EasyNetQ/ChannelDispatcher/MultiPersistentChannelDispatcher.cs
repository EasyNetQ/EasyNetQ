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
    private readonly Dictionary<PersistentChannelDispatchOptions, AsyncQueue<IPersistentChannel>> channelsPoolPerOptions = new();

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
        channelsPoolPerOptions.Add(
            PersistentChannelDispatchOptions.ProducerTopology,
            CreateChannelsPool(producerConnection, new PersistentChannelOptions(false))
        );
        channelsPoolPerOptions.Add(
            PersistentChannelDispatchOptions.ProducerPublish,
            CreateChannelsPool(producerConnection, new PersistentChannelOptions(false))
        );
        channelsPoolPerOptions.Add(
            PersistentChannelDispatchOptions.ProducerPublishWithConfirms,
            CreateChannelsPool(producerConnection, new PersistentChannelOptions(true))
        );
        channelsPoolPerOptions.Add(
            PersistentChannelDispatchOptions.ConsumerTopology,
            CreateChannelsPool(consumerConnection, new PersistentChannelOptions(false))
        );

        AsyncQueue<IPersistentChannel> CreateChannelsPool(IPersistentConnection connection, PersistentChannelOptions options)
        {
            var channels = Enumerable.Range(0, channelsCount).Select(_ => channelFactory.CreatePersistentChannel(connection, options));
            return new AsyncQueue<IPersistentChannel>(channels);
        }
    }

    /// <inheritdoc />
    public async ValueTask<TResult> InvokeAsync<TResult, TChannelAction>(
        TChannelAction channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
    {
        var channelsPool = channelsPoolPerOptions[options];
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

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var channelsPool in channelsPoolPerOptions.Values)
            while (channelsPool.TryDequeue(out var channel))
                channel.Dispose();
    }
}
