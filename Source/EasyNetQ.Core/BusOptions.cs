namespace EasyNetQ;

/// <summary>
///     Transport-neutral bus behaviour options consumed by the high-level facades (PubSub/Rpc/SendReceive/
///     Scheduler). The transport registration projects its own configuration (e.g. RabbitMQ's
///     ConnectionConfiguration) into this record.
/// </summary>
public sealed record BusOptions
{
    /// <summary>Operation timeout applied by the facades.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Default prefetch count for subscriptions.</summary>
    public ushort PrefetchCount { get; init; } = 50;

    /// <summary>Whether messages are persistent when the message type carries no [DeliveryMode] override.</summary>
    public bool PersistentMessages { get; init; } = true;

    /// <summary>Whether publisher confirms are enabled.</summary>
    public bool PublisherConfirms { get; init; }
}
