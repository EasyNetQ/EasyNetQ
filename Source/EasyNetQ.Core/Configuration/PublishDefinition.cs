using EasyNetQ.Pipeline;
using EasyNetQ.Transport;

namespace EasyNetQ.Configuration;

/// <summary>
///     Everything a fluent publish registration collects. The publisher declares the exchange, builds the publish
///     pipeline and routes registered message types through it.
/// </summary>
public sealed class PublishDefinition
{
    /// <summary>The target exchange</summary>
    public string Exchange { get; set; } = "";

    /// <summary>The exchange to declare before the first publish; null publishes to an existing exchange</summary>
    public ExchangeDefinition? ExchangeToDeclare { get; set; }

    /// <summary>Broker must route to at least one queue; null uses the bus default</summary>
    public bool? Mandatory { get; set; }

    /// <summary>Wait for a broker confirmation; null uses the bus default</summary>
    public bool? PublisherConfirms { get; set; }

    /// <summary>Message type registrations, applied to the route table at startup</summary>
    public List<Action<PublishRouteTable>> MessageRegistrations { get; } = new();

    /// <summary>Customizes the publish pipeline for this definition's routes</summary>
    public Action<PipelineBuilder<PublishContext>>? MessagePipeline { get; set; }
}
