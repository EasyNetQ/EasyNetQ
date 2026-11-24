using EasyNetQ.Pipeline;
using EasyNetQ.Transport;

namespace EasyNetQ.Configuration;

/// <summary>
///     A binding a consumer declares at startup; the destination is the consumer's queue (resolved after the
///     queue declaration, so server-named queues work)
/// </summary>
public sealed record ConsumerBinding(string Exchange, string RoutingKey, IDictionary<string, object>? Arguments = null);

/// <summary>
///     Everything a fluent consumer registration collects. The hosted startup declares the topology, builds the
///     consumer context and message pipeline, and starts the consumer on the transport.
/// </summary>
public sealed class ConsumerDefinition
{
    /// <summary>The queue to consume from</summary>
    public string Queue { get; set; } = "";

    /// <summary>The queue to declare at startup; null consumes from an existing queue</summary>
    public QueueDefinition? QueueToDeclare { get; set; }

    /// <summary>Exchanges to declare at startup</summary>
    public List<ExchangeDefinition> ExchangesToDeclare { get; } = new();

    /// <summary>Bindings to declare at startup</summary>
    public List<ConsumerBinding> Bindings { get; } = new();

    /// <summary>Prefetch; null uses the bus default</summary>
    public ushort? PrefetchCount { get; set; }

    /// <summary>Acknowledge automatically on delivery</summary>
    public bool AutoAck { get; set; }

    /// <summary>Handler registrations, applied to the consumer's handler table at startup</summary>
    public List<Action<IServiceProvider, HandlerTable>> HandlerRegistrations { get; } = new();

    /// <summary>Customizes the message pipeline after the typed dispatch steps are in place</summary>
    public Action<PipelineBuilder<ConsumeContext>>? MessagePipeline { get; set; }

    /// <summary>Transport-specific consumer settings, written into the consumer context's property bag</summary>
    public Action<ConsumerContext>? ConfigureContext { get; set; }
}
