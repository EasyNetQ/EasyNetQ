namespace EasyNetQ.Pipeline;

/// <summary>
///     What lower layers can see of a connection: readable properties, nothing writable
/// </summary>
public interface IConnectionView : IReadOnlyProperties
{
    /// <summary>
    ///     Name of the connection (e.g. "Producer" / "Consumer")
    /// </summary>
    string Name { get; }
}

/// <summary>
///     What lower layers can see of a channel (a queue/exchange topology scope on a connection)
/// </summary>
public interface IChannelView : IReadOnlyProperties
{
    /// <summary>
    ///     The connection the channel belongs to
    /// </summary>
    IConnectionView Connection { get; }
}

/// <summary>
///     What the message layer can see of the consumer it was delivered by
/// </summary>
public interface IConsumerView : IReadOnlyProperties
{
    /// <summary>
    ///     The channel the consumer runs on
    /// </summary>
    IChannelView Channel { get; }

    /// <summary>
    ///     Name of the queue being consumed
    /// </summary>
    string Queue { get; }

    /// <summary>
    ///     Prefetch count of the consumer
    /// </summary>
    ushort PrefetchCount { get; }

    /// <summary>
    ///     <see langword="true" /> when the transport acknowledges messages on delivery, ignoring the pipeline's decision
    /// </summary>
    bool AutoAck { get; }
}
