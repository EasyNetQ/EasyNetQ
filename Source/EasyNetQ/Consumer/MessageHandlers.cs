namespace EasyNetQ.Consumer;

/// <summary>
///     Represents a delegate which is called by consumer for every message
/// </summary>
public delegate ValueTask<AckDecision> MessageHandler(
    ReadOnlyMemory<byte> body,
    MessageProperties properties,
    MessageReceivedInfo receivedInfo,
    CancellationToken cancellationToken
);

/// <summary>
///     Represents a delegate which is called by consumer for every message
/// </summary>
public delegate ValueTask<AckDecision> IMessageHandler(
    IMessage message,
    MessageReceivedInfo receivedInfo,
    CancellationToken cancellationToken
);

/// <summary>
///     Represents a delegate which is called by consumer for every message
/// </summary>
public delegate ValueTask<AckDecision> IMessageHandler<in T>(
    IMessage<T> message,
    MessageReceivedInfo receivedInfo,
    CancellationToken cancellationToken
);
