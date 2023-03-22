using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;

namespace EasyNetQ.ChannelDispatcher;

/// <summary>
///     Invokes client commands using single channel
/// </summary>
public sealed class SinglePersistentChannelDispatcher : IPersistentChannelDispatcher, IDisposable
{
    private readonly Dictionary<PersistentChannelDispatchOptions, IPersistentChannel> channelPerOptions = new();

    /// <summary>
    /// Creates a dispatcher
    /// </summary>
    public SinglePersistentChannelDispatcher(
        IProducerConnection producerConnection,
        IConsumerConnection consumerConnection,
        IPersistentChannelFactory channelFactory
    )
    {
        channelPerOptions.Add(
            PersistentChannelDispatchOptions.ProducerTopology,
            channelFactory.CreatePersistentChannel(producerConnection, new PersistentChannelOptions(false))
        );
        channelPerOptions.Add(
            PersistentChannelDispatchOptions.ProducerPublish,
            channelFactory.CreatePersistentChannel(producerConnection, new PersistentChannelOptions(false))
        );
        channelPerOptions.Add(
            PersistentChannelDispatchOptions.ProducerPublishWithConfirms,
            channelFactory.CreatePersistentChannel(producerConnection, new PersistentChannelOptions(true))
        );
        channelPerOptions.Add(
            PersistentChannelDispatchOptions.ConsumerTopology,
            channelFactory.CreatePersistentChannel(consumerConnection, new PersistentChannelOptions(false))
        );
    }

    /// <inheritdoc />
    public ValueTask<TResult> InvokeAsync<TResult, TChannelAction>(
        TChannelAction channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>
        => channelPerOptions[options].InvokeChannelActionAsync<TResult, TChannelAction>(channelAction, cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var channel in channelPerOptions.Values)
            channel.Dispose();
    }
}
