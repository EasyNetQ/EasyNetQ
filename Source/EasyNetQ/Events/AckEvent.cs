namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a message is acked
/// </summary>
/// <param name="ReceiveInfo">Acked message received info</param>
/// <param name="Properties">Acked message properties</param>
/// <param name="Body">Acked message body</param>
/// <param name="AckResult">Ack result of message</param>
public readonly record struct AckEvent(in MessageReceivedInfo ReceiveInfo, in MessageProperties Properties, in ReadOnlyMemory<byte> Body, AckResult AckResult);


/// <summary>
///     Represents various ack results
/// </summary>
public enum AckResult
{
    /// <summary>
    ///     Message is acknowledged
    /// </summary>
    Ack,

    /// <summary>
    ///     Message is rejected
    /// </summary>
    Nack,

    /// <summary>
    ///     Message is failed to process
    /// </summary>
    Exception
}
