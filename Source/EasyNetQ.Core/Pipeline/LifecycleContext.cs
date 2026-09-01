namespace EasyNetQ.Pipeline;

/// <summary>
///     The layer a lifecycle event belongs to
/// </summary>
public enum LifecycleLayer : byte
{
    /// <summary>A connection-level event</summary>
    Connection,

    /// <summary>A channel-level event</summary>
    Channel,

    /// <summary>A consumer-level event</summary>
    Consumer,
}

/// <summary>
///     A lifecycle event name. Transports may define their own beyond the well-known ones.
/// </summary>
public readonly record struct LifecycleEvent(string Name)
{
    /// <summary>The connection is established</summary>
    public static readonly LifecycleEvent Connected = new("Connected");

    /// <summary>The connection recovered after a failure</summary>
    public static readonly LifecycleEvent Recovered = new("Recovered");

    /// <summary>The connection was lost</summary>
    public static readonly LifecycleEvent Disconnected = new("Disconnected");

    /// <summary>The broker blocked the connection</summary>
    public static readonly LifecycleEvent Blocked = new("Blocked");

    /// <summary>The broker unblocked the connection</summary>
    public static readonly LifecycleEvent Unblocked = new("Unblocked");

    /// <summary>An automatic recovery attempt failed; recovery keeps retrying</summary>
    public static readonly LifecycleEvent RecoveryError = new("RecoveryError");

    /// <summary>A user callback threw</summary>
    public static readonly LifecycleEvent CallbackError = new("CallbackError");

    /// <summary>A consumer started consuming</summary>
    public static readonly LifecycleEvent Started = new("Started");

    /// <summary>A consumer stopped consuming</summary>
    public static readonly LifecycleEvent Stopped = new("Stopped");

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>
///     Context of one lifecycle event. Parented to the connection, channel or consumer context the event belongs
///     to, so steps can read that layer's properties. Not pooled: lifecycle events are rare.
/// </summary>
public sealed class LifecycleContext : LayerContext
{
    /// <summary>
    ///     Creates the context under <paramref name="scope" /> (the connection, channel or consumer context)
    /// </summary>
    public LifecycleContext(LayerContext scope) : base(scope)
    {
    }

    /// <summary>The layer the event belongs to</summary>
    public LifecycleLayer Layer { get; set; }

    /// <summary>The event</summary>
    public LifecycleEvent Event { get; set; }

    /// <summary>Human-readable reason, when the source provides one (e.g. a disconnect or block reason)</summary>
    public string? Reason { get; set; }

    /// <summary>The failure, for error events</summary>
    public Exception? Error { get; set; }

    /// <summary>Cancellation for handling this event</summary>
    public CancellationToken CancellationToken { get; set; }
}
