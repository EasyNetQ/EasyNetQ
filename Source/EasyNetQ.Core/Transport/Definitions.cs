namespace EasyNetQ.Transport;

/// <summary>
///     An exchange to declare. <see cref="Arguments" /> carries transport-specific settings.
/// </summary>
public sealed record ExchangeDefinition(string Name, string Type = "topic", bool Durable = true, bool AutoDelete = false)
{
    /// <summary>Transport-specific arguments (e.g. alternate exchange)</summary>
    public IDictionary<string, object>? Arguments { get; init; }
}

/// <summary>
///     A queue to declare. An empty name requests a server-generated one. <see cref="Arguments" /> carries
///     transport-specific settings (queue type, dead lettering, TTLs, ...).
/// </summary>
public sealed record QueueDefinition(string Name = "", bool Durable = true, bool Exclusive = false, bool AutoDelete = false)
{
    /// <summary>Transport-specific arguments</summary>
    public IDictionary<string, object>? Arguments { get; init; }
}

/// <summary>
///     A binding from <paramref name="Source" /> (an exchange) to <paramref name="Destination" /> (a queue, or an
///     exchange when <paramref name="DestinationIsExchange" />).
/// </summary>
public sealed record BindingDefinition(string Source, string Destination, string RoutingKey, bool DestinationIsExchange = false)
{
    /// <summary>Transport-specific arguments (e.g. header-exchange match rules)</summary>
    public IDictionary<string, object>? Arguments { get; init; }
}
