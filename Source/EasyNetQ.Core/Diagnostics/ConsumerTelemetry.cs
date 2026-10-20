namespace EasyNetQ.Diagnostics;

/// <summary>
///     Per-consumer telemetry invariants, computed once when the consumer is configured and stored on the
///     consumer layer's property bag (<see cref="Pipeline.Keys.ConsumerTelemetry" />) so per-message telemetry
///     never rebuilds them
/// </summary>
public sealed class ConsumerTelemetry
{
    /// <summary>
    ///     Precomputes the invariants for a consumer of <paramref name="queue" />
    /// </summary>
    public ConsumerTelemetry(string queue, string messagingSystem)
    {
        Queue = queue;
        MessagingSystem = messagingSystem;
        SpanName = $"process {queue}";
    }

    /// <summary>The queue the consumer consumes from</summary>
    public string Queue { get; }

    /// <summary>Value for the messaging.system attribute</summary>
    public string MessagingSystem { get; }

    /// <summary>The span name, "process {queue}"</summary>
    public string SpanName { get; }
}
