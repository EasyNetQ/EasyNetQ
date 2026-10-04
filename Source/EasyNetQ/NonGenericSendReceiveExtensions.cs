using System.Diagnostics.CodeAnalysis;

namespace EasyNetQ;

/// <summary>
///     Various non-generic extensions for <see cref="ISendReceive"/>
/// </summary>
public static class NonGenericSendReceiveExtensions
{
    /// <summary>
    /// Send a message directly to a queue
    /// </summary>
    /// <param name="sendReceive">The sendReceive instance</param>
    /// <param name="queue">The queue to send to</param>
    /// <param name="message">The message</param>
    /// <param name="messageType">The message type</param>
    /// <param name="cancellationToken">The cancellation token</param>
    [RequiresDynamicCode(NonGenericBridge.RequiresDynamicCodeMessage)]
    public static Task SendAsync(
        this ISendReceive sendReceive,
        string queue,
        object message,
        Type messageType,
        CancellationToken cancellationToken = default
    ) => sendReceive.SendAsync(queue, message, messageType, _ => { }, cancellationToken);

    /// <summary>
    /// Send a message directly to a queue
    /// </summary>
    /// <param name="sendReceive">The sendReceive instance</param>
    /// <param name="queue">The queue to send to</param>
    /// <param name="message">The message</param>
    /// <param name="messageType">The message type</param>
    /// <param name="configure">
    ///     Fluent configuration e.g. x => x.WithPriority(2)
    /// </param>
    /// <param name="cancellationToken">The cancellation token</param>
    [RequiresDynamicCode(NonGenericBridge.RequiresDynamicCodeMessage)]
    public static Task SendAsync(
        this ISendReceive sendReceive,
        string queue,
        object message,
        Type messageType,
        Action<ISendConfiguration> configure,
        CancellationToken cancellationToken = default
    ) => NonGenericBridge.Get(messageType).SendViaAsync(sendReceive, queue, message, configure, cancellationToken);
}
