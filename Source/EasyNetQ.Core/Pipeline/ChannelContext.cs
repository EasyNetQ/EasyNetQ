namespace EasyNetQ.Pipeline;

/// <summary>
///     Channel layer: a queue/exchange topology scope on a connection. In RabbitMQ terms this is an AMQP channel
///     (one per consumer configuration, or the publish channel scope on the producer side).
/// </summary>
public sealed class ChannelContext : LayerContext, IChannelView
{
    /// <summary>
    ///     Creates a channel context on <paramref name="connection" />
    /// </summary>
    public ChannelContext(ConnectionContext connection) : base(connection)
    {
        Connection = connection;
    }

    /// <inheritdoc />
    public IConnectionView Connection { get; }
}
