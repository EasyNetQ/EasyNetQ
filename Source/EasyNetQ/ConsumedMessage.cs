namespace EasyNetQ;

/// <summary>
///     Represents a consumed message
/// </summary>
public readonly record struct ConsumedMessage(in MessageReceivedInfo ReceivedInfo, in MessageProperties Properties, in ReadOnlyMemory<byte> Body);
