namespace EasyNetQ.Pipeline;

/// <summary>
///     Well-known typed property keys. Set them on any layer (connection, channel, consumer, message); readers see
///     the nearest value up the hierarchy.
/// </summary>
public static class Keys
{
    /// <summary>
    ///     Overrides the message serializer for the layer it is set on (e.g. one queue using MessagePack while the
    ///     rest of the bus uses JSON)
    /// </summary>
    public static readonly PropertyKey<IMessageSerializer> Serializer = new("EasyNetQ.Serializer");

    /// <summary>
    ///     Per-consumer telemetry invariants, set on the consumer layer when the consumer is configured
    /// </summary>
    public static readonly PropertyKey<Diagnostics.ConsumerTelemetry> ConsumerTelemetry = new("EasyNetQ.ConsumerTelemetry");

    /// <summary>
    ///     The intent of a connection layer (producer or consumer side), read by transports on ConnectAsync
    /// </summary>
    public static readonly PropertyKey<Persistent.PersistentConnectionType> ConnectionType = new("EasyNetQ.ConnectionType");
}
