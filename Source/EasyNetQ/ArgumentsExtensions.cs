namespace EasyNetQ;

public static class ArgumentsExtensions
{
    # region Queue

    /// <summary>
    ///     Sets queue type.
    ///     Valid types are classic and quorum, see <see cref="QueueType"/>.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="queueType">The queue type to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithQueueType(this IDictionary<string, object> arguments, string queueType) =>
        arguments.WithArgument(Argument.QueueType, queueType);

    /// <summary>
    ///     Sets queue mode.
    ///     Valid modes are default and lazy, see <see cref="QueueMode"/>.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="queueMode">The queue mode to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithQueueMode(this IDictionary<string, object> arguments, string queueMode) =>
        arguments.WithArgument(Argument.QueueMode, queueMode);

    /// <summary>
    ///     Sets expires of the queue.
    ///     Determines how long a queue can remain unused before it is automatically deleted by the server.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="expiresMs">The expires to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithExpires(this IDictionary<string, object> arguments, int expiresMs) =>
        arguments.WithArgument(Argument.Expires, expiresMs);

    /// <summary>
    ///     Sets expires of the queue.
    ///     Determines how long a queue can remain unused before it is automatically deleted by the server.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="expires">The expires to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithExpires(this IDictionary<string, object> arguments, TimeSpan expires) =>
        arguments.WithArgument(Argument.Expires, (int)expires.TotalMilliseconds);

    /// <summary>
    ///     Sets maximum priority the queue should support.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="maxPriority">The maximum priority to set, should be a positive integer between 1 and 255</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithMaxPriority(this IDictionary<string, object> arguments, byte maxPriority) =>
        arguments.WithArgument(Argument.MaxPriority, maxPriority);

    /// <summary>
    ///     Sets maximum queue length. The maximum number of ready messages that may exist on the queue.
    ///     Messages will be dropped or dead-lettered from the front of the queue to make room for new
    ///     messages once the limit is reached.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="maxLength">The maximum length to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithMaxLength(this IDictionary<string, object> arguments, int maxLength) =>
        arguments.WithArgument(Argument.MaxLength, maxLength);

    /// <summary>
    ///     Sets maximum queue length in bytes. The maximum size of the queue in bytes.
    ///     Messages will be dropped or dead-lettered from the front of the queue to make room
    ///     for new messages once the limit is reached.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="maxLengthBytes">The maximum queue length to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithMaxLengthBytes(this IDictionary<string, object> arguments, int maxLengthBytes) =>
        arguments.WithArgument(Argument.MaxLengthBytes, maxLengthBytes);

    /// <summary>
    ///     Enables single active consumer.
    ///     If set, makes sure only one consumer at a time consumes from the queue and fails
    ///     over to another registered consumer in case the active one is cancelled or dies.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="singleActiveConsumer"><see langword="true"/> if a queue has a single active consumer</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithSingleActiveConsumer(this IDictionary<string, object> arguments, bool singleActiveConsumer = true) =>
        arguments.WithArgument(Argument.SingleActiveConsumer, singleActiveConsumer);

    /// <summary>
    ///     Sets message TTL.
    ///     Determines how long a message published to a queue can live before it is discarded by the server.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="messageTtlMs">The message TTL to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithMessageTtl(this IDictionary<string, object> arguments, int messageTtlMs) =>
        arguments.WithArgument(Argument.MessageTtl, messageTtlMs);

    /// <summary>
    ///     Sets message TTL.
    ///     Determines how long a message published to a queue can live before it is discarded by the server.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="messageTtl">The message TTL to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithMessageTtl(this IDictionary<string, object> arguments, TimeSpan messageTtl) =>
        arguments.WithArgument(Argument.MessageTtl, (int)messageTtl.TotalMilliseconds);

    /// <summary>
    ///     Sets dead letter exchange for queue.
    ///     An exchange to which messages will be republished if they are rejected or expire.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="deadLetterExchange">The dead letter exchange to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithDeadLetterExchange(this IDictionary<string, object> arguments, string deadLetterExchange) =>
        arguments.WithArgument(Argument.DeadLetterExchange, deadLetterExchange);

    /// <summary>
    ///     Sets dead letter routing key.
    ///     If set, will route message when this message is dead-lettered with the routing key specified.
    ///     If not set, message will be routed with the same routing keys they were originally published with.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="deadLetterRoutingKey">The dead letter routing key to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithDeadLetterRoutingKey(this IDictionary<string, object> arguments, string deadLetterRoutingKey) =>
        arguments.WithArgument(Argument.DeadLetterRoutingKey, deadLetterRoutingKey);

    /// <summary>
    ///     Sets queue master locator.
    ///     Valid types are min-masters, client-local and random, see <see cref="QueueMasterLocator"/>.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="queueMasterLocator">The queue master locator to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithQueueMasterLocator(this IDictionary<string, object> arguments, string queueMasterLocator) =>
        arguments.WithArgument(Argument.QueueMasterLocator, queueMasterLocator);

    /// <summary>
    ///     Sets dead letter strategy.
    ///     Valid types are at-least-once and at-most-once, see <see cref="DeadLetterStrategy"/>.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="deadLetterStrategy">The dead letter strategy to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithDeadLetterStrategy(this IDictionary<string, object> arguments, string deadLetterStrategy) =>
        arguments.WithArgument(Argument.DeadLetterStrategy, deadLetterStrategy);

    /// <summary>
    ///     Sets overflow type to configure overflow behaviour.
    ///     Valid types are drop-head, reject-publish and reject-publish-dlx, see <see cref="OverflowType"/>.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="overflowType">The overflow type to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static IDictionary<string, object> WithOverflowType(this IDictionary<string, object> arguments, string overflowType) =>
        arguments.WithArgument(Argument.Overflow, overflowType);

    # endregion

    # region Exchange

    public static IDictionary<string, object> WithAlternateExchange(this IDictionary<string, object> arguments, string alternateExchange) =>
        arguments.WithArgument(Argument.AlternateExchange, alternateExchange);

    public static IDictionary<string, object> WithDelayedType(this IDictionary<string, object> arguments, string delayedType) =>
        arguments.WithArgument(Argument.DelayedType, delayedType);

    # endregion

    public static IDictionary<string, object> WithArgument(this IDictionary<string, object> arguments, string name, object value)
    {
        arguments[name] = value;
        return arguments;
    }
}
