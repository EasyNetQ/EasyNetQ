namespace EasyNetQ.Pipeline;

/// <summary>
///     Message layer of the publish pipeline: everything needed to publish one message. Instances are pooled;
///     transports may derive typed contexts.
/// </summary>
public class PublishContext : LayerContext
{
    /// <summary>
    ///     Creates a publish context on <paramref name="channel" />
    /// </summary>
    public PublishContext(ChannelContext channel) : base(channel)
    {
        Channel = channel;
    }

    /// <summary>
    ///     The publish channel scope (read-only view)
    /// </summary>
    public IChannelView Channel { get; }

    /// <summary>
    ///     The connection being published on (read-only view)
    /// </summary>
    public IConnectionView Connection => Channel.Connection;

    /// <summary>
    ///     Target exchange
    /// </summary>
    public string Exchange { get; set; } = "";

    /// <summary>
    ///     Routing key
    /// </summary>
    public string RoutingKey { get; set; } = "";

    /// <summary>
    ///     Whether the broker must route the message to at least one queue
    /// </summary>
    public bool Mandatory { get; set; }

    /// <summary>
    ///     Whether to wait for a broker confirmation
    /// </summary>
    public bool PublisherConfirms { get; set; }

    /// <summary>
    ///     Message properties; middleware may replace them
    /// </summary>
    public MessageProperties Properties { get; set; }

    /// <summary>
    ///     Serialized body; middleware may replace it
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; set; }

    /// <summary>
    ///     Cancellation for this publish (includes the configured timeout)
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <inheritdoc />
    protected internal override void Reset()
    {
        base.Reset();
        Exchange = "";
        RoutingKey = "";
        Mandatory = false;
        PublisherConfirms = false;
        Properties = default;
        Body = default;
        CancellationToken = default;
    }
}
