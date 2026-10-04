namespace EasyNetQ;

/// <summary>
///     Represents a publishing message
/// </summary>
public readonly record struct ProducedMessage(in MessageProperties Properties, in ReadOnlyMemory<byte> Body);
