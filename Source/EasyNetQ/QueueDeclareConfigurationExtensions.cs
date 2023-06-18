using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Various fluent extensions for <see cref="IQueueDeclareConfiguration"/>
/// </summary>
public static class QueueDeclareConfigurationExtensions
{
    /// <summary>
    ///     Sets maximum priority the queue should support.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="maxPriority">The maximum priority to set, should be a positive integer between 1 and 255</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithMaxPriority(this IQueueDeclareConfiguration configuration, byte maxPriority) =>
        configuration.WithArgument(Argument.MaxPriority, maxPriority);

    /// <summary>
    ///     Sets maximum queue length. The maximum number of ready messages that may exist on the queue.
    ///     Messages will be dropped or dead-lettered from the front of the queue to make room for new
    ///     messages once the limit is reached.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="maxLength">The maximum length to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithMaxLength(this IQueueDeclareConfiguration configuration, int maxLength)
        => configuration.WithArgument(Argument.MaxLength, maxLength);

    /// <summary>
    ///     Sets maximum queue length in bytes. The maximum size of the queue in bytes.
    ///     Messages will be dropped or dead-lettered from the front of the queue to make room
    ///     for new messages once the limit is reached.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="maxLengthBytes">The maximum queue length to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithMaxLengthBytes(this IQueueDeclareConfiguration configuration, int maxLengthBytes) =>
        configuration.WithArgument(Argument.MaxLengthBytes, maxLengthBytes);

    /// <summary>
    ///     Sets overflow type to configure overflow behaviour.
    ///     Valid types are drop-head, reject-publish and reject-publish-dlx, see <see cref="OverflowType"/>.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="overflowType">The overflow type to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithOverflowType(this IQueueDeclareConfiguration configuration, string overflowType = OverflowType.DropHead) =>
        configuration.WithArgument(Argument.Overflow, overflowType);

    /// <summary>
    ///     Sets expires of the queue.
    ///     Determines how long a queue can remain unused before it is automatically deleted by the server.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="expires">The expires to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithExpires(this IQueueDeclareConfiguration configuration, TimeSpan expires) =>
        configuration.WithArgument(Argument.Expires, (int)expires.TotalMilliseconds);

    /// <summary>
    ///     Sets message TTL.
    ///     Determines how long a message published to a queue can live before it is discarded by the server.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="messageTtl">The message TTL to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithMessageTtl(this IQueueDeclareConfiguration configuration, TimeSpan messageTtl) =>
        configuration.WithArgument(Argument.MessageTtl, (int)messageTtl.TotalMilliseconds);

    /// <summary>
    ///     Sets dead letter exchange for queue.
    ///     An exchange to which messages will be republished if they are rejected or expire.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="deadLetterExchange">The dead letter exchange to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithDeadLetterExchange(this IQueueDeclareConfiguration configuration, Exchange deadLetterExchange) =>
        configuration.WithArgument(Argument.DeadLetterExchange, deadLetterExchange.Name);

    /// <summary>
    ///     Sets dead letter routing key.
    ///     If set, will route message when this message is dead-lettered with the routing key specified.
    ///     If not set, message will be routed with the same routing keys they were originally published with.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="deadLetterRoutingKey">The dead letter routing key to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithDeadLetterRoutingKey(this IQueueDeclareConfiguration configuration, string deadLetterRoutingKey) =>
        configuration.WithArgument(Argument.DeadLetterRoutingKey, deadLetterRoutingKey);

    /// <summary>
    ///     Sets queue mode.
    ///     Valid modes are default and lazy, see <see cref="QueueMode"/>.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="queueMode">The queue mode to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithQueueMode(this IQueueDeclareConfiguration configuration, string queueMode = QueueMode.Default) =>
        configuration.WithArgument(Argument.QueueMode, queueMode);

    /// <summary>
    ///     Sets queue type.
    ///     Valid types are classic and quorum, see <see cref="QueueType"/>.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="queueType">The queue type to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithQueueType(this IQueueDeclareConfiguration configuration, string queueType = QueueType.Classic) =>
        configuration.WithArgument(Argument.QueueType, queueType);

    /// <summary>
    ///     Enables single active consumer.
    ///     If set, makes sure only one consumer at a time consumes from the queue and fails
    ///     over to another registered consumer in case the active one is cancelled or dies.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="singleActiveConsumer"><see langword="true"/> if a queue has a single active consumer</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithSingleActiveConsumer(this IQueueDeclareConfiguration configuration, bool singleActiveConsumer = true) =>
        configuration.WithArgument(Argument.SingleActiveConsumer, singleActiveConsumer);

    /// <summary>
    ///     Sets queue master locator.
    ///     Valid types are min-masters, client-local and random, see <see cref="QueueMasterLocator"/>.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="queueMasterLocator">The queue master locator to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithQueueMasterLocator(this IQueueDeclareConfiguration configuration, string queueMasterLocator = QueueMasterLocator.MinMasters) =>
        configuration.WithArgument(Argument.QueueMasterLocator, queueMasterLocator);

    /// <summary>
    ///     Sets dead letter strategy.
    ///     Valid types are at-least-once and at-most-once, see <see cref="DeadLetterStrategy"/>.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="deadLetterStrategy">The dead letter strategy to set</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    public static IQueueDeclareConfiguration WithDeadLetterStrategy(this IQueueDeclareConfiguration configuration, string deadLetterStrategy = DeadLetterStrategy.AtMostOnce) =>
        configuration.WithArgument(Argument.DeadLetterStrategy, deadLetterStrategy);
}
