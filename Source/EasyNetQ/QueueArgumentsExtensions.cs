namespace EasyNetQ;

public readonly struct QueueArgumentsBuilder
{
    public static QueueArgumentsBuilder Empty => default;

    public static QueueArgumentsBuilder From(IDictionary<string, object> arguments) => new(arguments);

    private readonly IDictionary<string, object>? arguments;

    private QueueArgumentsBuilder(IDictionary<string, object> arguments) => this.arguments = arguments;

    public QueueArgumentsBuilder WithArgument(string name, object value)
    {
        var newArguments = arguments ?? new Dictionary<string, object>();
        newArguments[name] = value;
        return new QueueArgumentsBuilder(newArguments);
    }

    public IDictionary<string, object>? Build() => arguments;
}

public static class QueueQueueArgumentsBuilderExtensions
{
    /// <summary>
    ///     Sets queue type.
    ///     Valid types are classic and quorum, see <see cref="QueueType"/>.
    /// </summary>
    /// <param name="builder">The queue arguments</param>
    /// <param name="queueType">The queue type to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithQueueType(this QueueArgumentsBuilder builder, string queueType) =>
        builder.WithArgument(QueueArgument.QueueType, queueType);

    /// <summary>
    ///     Sets queue mode.
    ///     Valid modes are default and lazy, see <see cref="QueueMode"/>.
    /// </summary>
    /// <param name="builder">The queue arguments</param>
    /// <param name="queueMode">The queue mode to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithQueueMode(this QueueArgumentsBuilder builder, string queueMode) =>
        builder.WithArgument(QueueArgument.QueueMode, queueMode);

    /// <summary>
    ///     Sets expires of the queue.
    ///     Determines how long a queue can remain unused before it is automatically deleted by the server.
    /// </summary>
    /// <param name="builder">The queue arguments</param>
    /// <param name="expires">The expires to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithExpires(this QueueArgumentsBuilder builder, TimeSpan expires)
    {
        return builder.WithArgument(QueueArgument.Expires, (int)expires.TotalMilliseconds);
    }

    /// <summary>
    ///     Sets maximum priority the queue should support.
    /// </summary>
    /// <param name="builder">The queue arguments</param>
    /// <param name="maxPriority">The maximum priority to set, should be a positive integer between 1 and 255</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithMaxPriority(this QueueArgumentsBuilder builder, byte maxPriority) =>
        builder.WithArgument(QueueArgument.MaxPriority, maxPriority);

    /// <summary>
    ///     Sets maximum queue length. The maximum number of ready messages that may exist on the queue.
    ///     Messages will be dropped or dead-lettered from the front of the queue to make room for new
    ///     messages once the limit is reached.
    /// </summary>
    /// <param name="builder">The queue arguments</param>
    /// <param name="maxLength">The maximum length to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithMaxLength(this QueueArgumentsBuilder builder, int maxLength) =>
        builder.WithArgument(QueueArgument.MaxLength, maxLength);

    /// <summary>
    ///     Sets maximum queue length in bytes. The maximum size of the queue in bytes.
    ///     Messages will be dropped or dead-lettered from the front of the queue to make room
    ///     for new messages once the limit is reached.
    /// </summary>
    /// <param name="builder">The queue arguments</param>
    /// <param name="maxLengthBytes">The maximum queue length to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithMaxLengthBytes(this QueueArgumentsBuilder builder, int maxLengthBytes) =>
        builder.WithArgument(QueueArgument.MaxLengthBytes, maxLengthBytes);

    /// <summary>
    ///     Enables single active consumer.
    ///     If set, makes sure only one consumer at a time consumes from the queue and fails
    ///     over to another registered consumer in case the active one is cancelled or dies.
    /// </summary>
    /// <param name="builder">The configuration instance</param>
    /// <param name="singleActiveConsumer"><see langword="true"/> if a queue has a single active consumer</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithSingleActiveConsumer(this QueueArgumentsBuilder builder, bool singleActiveConsumer) =>
        builder.WithArgument(QueueArgument.SingleActiveConsumer, singleActiveConsumer);

    /// <summary>
    ///     Sets message TTL.
    ///     Determines how long a message published to a queue can live before it is discarded by the server.
    /// </summary>
    /// <param name="builder">The configuration instance</param>
    /// <param name="messageTtl">The message TTL to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithMessageTtl(this QueueArgumentsBuilder builder, TimeSpan messageTtl) =>
        builder.WithArgument(QueueArgument.MessageTtl, (int)messageTtl.TotalMilliseconds);

    /// <summary>
    ///     Sets dead letter exchange for queue.
    ///     An exchange to which messages will be republished if they are rejected or expire.
    /// </summary>
    /// <param name="builder">The configuration instance</param>
    /// <param name="deadLetterExchange">The dead letter exchange to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithDeadLetterExchange(this QueueArgumentsBuilder builder, string deadLetterExchange) =>
        builder.WithArgument(QueueArgument.DeadLetterExchange, deadLetterExchange);

    /// <summary>
    ///     Sets dead letter routing key.
    ///     If set, will route message when this message is dead-lettered with the routing key specified.
    ///     If not set, message will be routed with the same routing keys they were originally published with.
    /// </summary>
    /// <param name="builder">The configuration instance</param>
    /// <param name="deadLetterRoutingKey">The dead letter routing key to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithDeadLetterRoutingKey(this QueueArgumentsBuilder builder, string deadLetterRoutingKey) =>
        builder.WithArgument(QueueArgument.DeadLetterRoutingKey, deadLetterRoutingKey);

    /// <summary>
    ///     Sets queue master locator.
    ///     Valid types are min-masters, client-local and random, see <see cref="QueueMasterLocator"/>.
    /// </summary>
    /// <param name="builder">The configuration instance</param>
    /// <param name="queueMasterLocator">The queue master locator to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithQueueMasterLocator(this QueueArgumentsBuilder builder, string queueMasterLocator) =>
        builder.WithArgument(QueueArgument.QueueMasterLocator, queueMasterLocator);

    /// <summary>
    ///     Sets dead letter strategy.
    ///     Valid types are at-least-once and at-most-once, see <see cref="DeadLetterStrategy"/>.
    /// </summary>
    /// <param name="builder">The configuration instance</param>
    /// <param name="deadLetterStrategy">The dead letter strategy to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithDeadLetterStrategy(this QueueArgumentsBuilder builder, string deadLetterStrategy) =>
        builder.WithArgument(QueueArgument.DeadLetterStrategy, deadLetterStrategy);

    /// <summary>
    ///     Sets overflow type to configure overflow behaviour.
    ///     Valid types are drop-head, reject-publish and reject-publish-dlx, see <see cref="OverflowType"/>.
    /// </summary>
    /// <param name="builder">The configuration instance</param>
    /// <param name="overflowType">The overflow type to set</param>
    /// <returns>The same <paramref name="builder"/></returns>
    public static QueueArgumentsBuilder WithOverflowType(this QueueArgumentsBuilder builder, string overflowType) =>
        builder.WithArgument(QueueArgument.Overflow, overflowType);
}
