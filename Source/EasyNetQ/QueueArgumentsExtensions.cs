namespace EasyNetQ;

using Arguments = IDictionary<string, object>;

public static class QueueArgumentsExtensions
{
    /// <summary>
    ///     Sets queue type.
    ///     Valid types are classic and quorum, see <see cref="QueueType"/>.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="queueType">The queue type to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueType(this Arguments arguments, string queueType = QueueType.Classic) =>
        arguments.WithQueueArgument(QueueArgument.QueueType, queueType);

    /// <summary>
    ///     Sets queue mode.
    ///     Valid modes are default and lazy, see <see cref="QueueMode"/>.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="queueMode">The queue mode to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueMode(this Arguments arguments, string queueMode = QueueMode.Default) =>
        arguments.WithQueueArgument(QueueArgument.QueueMode, queueMode);

    /// <summary>
    ///     Sets expires of the queue.
    ///     Determines how long a queue can remain unused before it is automatically deleted by the server.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="expires">The expires to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueExpires(this Arguments arguments, TimeSpan expires) =>
        arguments.WithQueueArgument(QueueArgument.Expires, (int)expires.TotalMilliseconds);

    /// <summary>
    ///     Sets maximum priority the queue should support.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="maxPriority">The maximum priority to set, should be a positive integer between 1 and 255</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueMaxPriority(this Arguments arguments, byte maxPriority) =>
        arguments.WithQueueArgument(QueueArgument.MaxPriority, maxPriority);

    /// <summary>
    ///     Sets maximum queue length. The maximum number of ready messages that may exist on the queue.
    ///     Messages will be dropped or dead-lettered from the front of the queue to make room for new
    ///     messages once the limit is reached.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="maxLength">The maximum length to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueMaxLength(this Arguments arguments, int maxLength) =>
        arguments.WithQueueArgument(QueueArgument.MaxLength, maxLength);

    /// <summary>
    ///     Sets maximum queue length in bytes. The maximum size of the queue in bytes.
    ///     Messages will be dropped or dead-lettered from the front of the queue to make room
    ///     for new messages once the limit is reached.
    /// </summary>
    /// <param name="arguments">The queue arguments</param>
    /// <param name="maxLengthBytes">The maximum queue length to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueMaxLengthBytes(this Arguments arguments, int maxLengthBytes) =>
        arguments.WithQueueArgument(QueueArgument.MaxLengthBytes, maxLengthBytes);

    /// <summary>
    ///     Enables single active consumer.
    ///     If set, makes sure only one consumer at a time consumes from the queue and fails
    ///     over to another registered consumer in case the active one is cancelled or dies.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="singleActiveConsumer"><see langword="true"/> if a queue has a single active consumer</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueSingleActiveConsumer(this Arguments arguments, bool singleActiveConsumer = true) =>
        arguments.WithQueueArgument(QueueArgument.SingleActiveConsumer, singleActiveConsumer);

    /// <summary>
    ///     Sets message TTL.
    ///     Determines how long a message published to a queue can live before it is discarded by the server.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="messageTtl">The message TTL to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueMessageTtl(this Arguments arguments, TimeSpan messageTtl) =>
        arguments.WithQueueArgument(QueueArgument.MessageTtl, (int)messageTtl.TotalMilliseconds);

    /// <summary>
    ///     Sets dead letter exchange for queue.
    ///     An exchange to which messages will be republished if they are rejected or expire.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="deadLetterExchange">The dead letter exchange to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueDeadLetterExchange(this Arguments arguments, string deadLetterExchange) =>
        arguments.WithQueueArgument(QueueArgument.DeadLetterExchange, deadLetterExchange);

    /// <summary>
    ///     Sets dead letter routing key.
    ///     If set, will route message when this message is dead-lettered with the routing key specified.
    ///     If not set, message will be routed with the same routing keys they were originally published with.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="deadLetterRoutingKey">The dead letter routing key to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueDeadLetterRoutingKey(this Arguments arguments, string deadLetterRoutingKey) =>
        arguments.WithQueueArgument(QueueArgument.DeadLetterRoutingKey, deadLetterRoutingKey);

    /// <summary>
    ///     Sets queue master locator.
    ///     Valid types are min-masters, client-local and random, see <see cref="QueueMasterLocator"/>.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="queueMasterLocator">The queue master locator to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueMasterLocator(this Arguments arguments, string queueMasterLocator = QueueMasterLocator.MinMasters) =>
        arguments.WithQueueArgument(QueueArgument.QueueMasterLocator, queueMasterLocator);

    /// <summary>
    ///     Sets dead letter strategy.
    ///     Valid types are at-least-once and at-most-once, see <see cref="DeadLetterStrategy"/>.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="deadLetterStrategy">The dead letter strategy to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueDeadLetterStrategy(this Arguments arguments, string deadLetterStrategy = DeadLetterStrategy.AtMostOnce) =>
        arguments.WithQueueArgument(QueueArgument.DeadLetterStrategy, deadLetterStrategy);

    /// <summary>
    ///     Sets overflow type to configure overflow behaviour.
    ///     Valid types are drop-head, reject-publish and reject-publish-dlx, see <see cref="OverflowType"/>.
    /// </summary>
    /// <param name="arguments">The configuration instance</param>
    /// <param name="overflowType">The overflow type to set</param>
    /// <returns>The same <paramref name="arguments"/></returns>
    public static Arguments WithQueueOverflowType(this Arguments arguments, string overflowType = OverflowType.DropHead) =>
        arguments.WithQueueArgument(QueueArgument.Overflow, overflowType);

    public static Arguments WithQueueArgument(this Arguments arguments, string key, object value)
    {
        arguments[key] = value;
        return arguments;
    }
}
