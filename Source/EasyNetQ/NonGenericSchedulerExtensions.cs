using System.Diagnostics.CodeAnalysis;

namespace EasyNetQ;

/// <summary>
///     Various non-generic extensions for <see cref="IScheduler"/>
/// </summary>
public static class NonGenericSchedulerExtensions
{
    /// <summary>
    /// Schedule a message to be published at some time in the future
    /// </summary>
    /// <param name="scheduler">The scheduler instance</param>
    /// <param name="message">The message</param>
    /// <param name="messageType">The message type</param>
    /// <param name="delay">The delay for message to publish in future</param>
    /// <param name="cancellationToken">The cancellation token</param>
    [RequiresDynamicCode(NonGenericBridge.RequiresDynamicCodeMessage)]
    public static Task FuturePublishAsync(
        this IScheduler scheduler,
        object message,
        Type messageType,
        TimeSpan delay,
        CancellationToken cancellationToken = default
    ) => scheduler.FuturePublishAsync(message, messageType, delay, _ => { }, cancellationToken);

    /// <summary>
    /// Schedule a message to be published at some time in the future.
    /// </summary>
    /// <param name="scheduler">The scheduler instance</param>
    /// <param name="message">The message</param>
    /// <param name="messageType">The message type</param>
    /// <param name="delay">The delay for message to publish in future</param>
    /// <param name="configure">
    ///     Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
    /// </param>
    /// <param name="cancellationToken">The cancellation token</param>
    [RequiresDynamicCode(NonGenericBridge.RequiresDynamicCodeMessage)]
    public static Task FuturePublishAsync(
        this IScheduler scheduler,
        object message,
        Type messageType,
        TimeSpan delay,
        Action<IFuturePublishConfiguration> configure,
        CancellationToken cancellationToken = default
    ) => NonGenericBridge.Get(messageType).FuturePublishViaAsync(scheduler, message, delay, configure, cancellationToken);
}
