using EasyNetQ.Pipeline;

namespace EasyNetQ.Transport;

/// <summary>
///     Entry point of a transport implementation. The connection context carries the intent
///     (<see cref="Pipeline.Keys.ConnectionType" />); the returned connection is a logical connection - transports
///     may map it onto pooled physical connections.
/// </summary>
public interface ITransport
{
    /// <summary>
    ///     Returns the logical connection for <paramref name="context" />. May complete synchronously when the
    ///     transport connects lazily.
    /// </summary>
    ValueTask<ITransportConnection> ConnectAsync(ConnectionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
///     A logical connection. Channels opened from it inherit its intent (producer/consumer side).
/// </summary>
public interface ITransportConnection : IAsyncDisposable
{
    /// <summary>
    ///     Whether the underlying connection is currently established
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Connects eagerly; without this, transports may connect on first use
    /// </summary>
    Task EnsureConnectedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Opens a logical channel. Transports may map it onto pooled physical channels.
    /// </summary>
    ValueTask<ITransportChannel> OpenChannelAsync(ChannelContext context, CancellationToken cancellationToken = default);
}

/// <summary>
///     A logical channel: the unit publishes, consumers and topology operations go through
/// </summary>
public interface ITransportChannel : IAsyncDisposable
{
    /// <summary>
    ///     Publishes one message. Completes when the transport accepts it - with publisher confirms, when the
    ///     broker confirms it. Failures throw.
    /// </summary>
    ValueTask PublishAsync(PublishContext context);

    /// <summary>
    ///     Starts one consumer over the given per-queue consumer contexts (one context per queue; multi-queue
    ///     consumers share the channel). Each context carries prefetch, auto-ack, the message pipeline and
    ///     transport-specific extras in its property bag.
    /// </summary>
    ValueTask<ITransportConsumer> StartConsumerAsync(IReadOnlyCollection<ConsumerContext> consumers, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Topology operations, or null when the transport does not support declaring topology
    /// </summary>
    ITopology? Topology { get; }
}

/// <summary>
///     A running consumer; disposing stops it
/// </summary>
public interface ITransportConsumer : IAsyncDisposable;
