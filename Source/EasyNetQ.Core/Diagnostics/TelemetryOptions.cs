namespace EasyNetQ.Diagnostics;

/// <summary>
///     Controls trace-context propagation on the publish path
/// </summary>
public enum ContextPropagationMode
{
    /// <summary>Inject the current trace context into outgoing message headers when a trace is active</summary>
    Auto,

    /// <summary>Always run injection, even when no activity is current</summary>
    Always,

    /// <summary>Never inject trace context into outgoing message headers</summary>
    Never
}

/// <summary>
///     Telemetry behavior; register a customized instance to override the defaults
/// </summary>
public sealed class TelemetryOptions
{
    /// <summary>
    ///     Value of the messaging.system attribute; the transport sets it
    /// </summary>
    public string MessagingSystem { get; set; } = "rabbitmq";

    /// <summary>
    ///     Record easynetq.message.type on spans and metrics (low cardinality in typical apps)
    /// </summary>
    public bool RecordMessageType { get; set; } = true;

    /// <summary>
    ///     Record the routing key on spans (off by default: routing keys can be high-cardinality)
    /// </summary>
    public bool RecordRoutingKey { get; set; }

    /// <summary>
    ///     Record messaging.message.body.size on publish spans
    /// </summary>
    public bool RecordBodySize { get; set; }

    /// <summary>
    ///     How trace context is injected into outgoing message headers. The RabbitMQ client injects its own wire
    ///     span's context as well; this injection matters when the client's publisher source is not enabled.
    /// </summary>
    public ContextPropagationMode PropagateContext { get; set; } = ContextPropagationMode.Auto;
}
