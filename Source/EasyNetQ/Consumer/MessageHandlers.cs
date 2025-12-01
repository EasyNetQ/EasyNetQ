namespace EasyNetQ.Consumer;

/// <summary>
///     Represents a delegate which is called by consumer for every message
/// </summary>
public delegate Task<AckStrategyAsync> MessageHandler(
    ReadOnlyMemory<byte> body,
    MessageProperties properties,
    MessageReceivedInfo receivedInfo,
    CancellationToken cancellationToken
);

/// <summary>
///     Represents a delegate which is called by consumer for every message
/// </summary>
public delegate Task<AckStrategyAsync> IMessageHandler(
    IMessage message,
    MessageReceivedInfo receivedInfo,
    CancellationToken cancellationToken
);

/// <summary>
///     Represents a delegate which is called by consumer for every message
/// </summary>
public delegate Task<AckStrategyAsync> IMessageHandler<in T>(
    IMessage<T> message,
    MessageReceivedInfo receivedInfo,
    CancellationToken cancellationToken
);
