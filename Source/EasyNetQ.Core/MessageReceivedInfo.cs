namespace EasyNetQ;

/// <summary>
///     Represents various properties of a received message
/// </summary>
public readonly record struct MessageReceivedInfo(
    string ConsumerTag,
    ulong DeliveryTag,
    bool Redelivered,
    string Exchange,
    string RoutingKey,
    string Queue
);
