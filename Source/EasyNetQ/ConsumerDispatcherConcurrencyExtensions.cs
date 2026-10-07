namespace EasyNetQ;

/// <summary>
///     Extensions to set the dispatch concurrency of a single consumer, overriding
///     <see cref="ConnectionConfiguration.ConsumerDispatcherConcurrency"/> for that consumer only.
/// </summary>
/// <remarks>
///     The value is applied to the consumer's channel: it is shared by all the queues consumed by one consumer.
///     The messages handled at the same time are also limited by the prefetch count, unless it is zero (unlimited) or messages
///     are auto acknowledged. The prefetch count applies to each queue of the consumer, because EasyNetQ consumes every queue
///     with an AMQP consumer of its own.
///     For concurrency greater than one the consumer could process messages in any order, not in the order it receives them,
///     and its handlers need to be thread/concurrency safe.
/// </remarks>
public static class ConsumerDispatcherConcurrencyExtensions
{
    /// <summary>
    ///     Sets how many messages the consumer handles concurrently, overriding
    ///     <see cref="ConnectionConfiguration.ConsumerDispatcherConcurrency"/> for this consumer only.
    ///     The value is shared by all the queues of the consumer, because they are consumed on one channel.
    /// </summary>
    /// <remarks>For concurrency greater than one, the consumer could process messages in any order, not in the order it receives them</remarks>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="consumerDispatcherConcurrency">The concurrency to set, greater than zero</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="consumerDispatcherConcurrency"/> is zero</exception>
    /// <exception cref="NotSupportedException"><paramref name="configuration"/> was not created by EasyNetQ</exception>
    public static IConsumeConfiguration WithConsumerDispatcherConcurrency(
        this IConsumeConfiguration configuration, ushort consumerDispatcherConcurrency
    ) => SetConsumerDispatcherConcurrency(configuration, consumerDispatcherConcurrency);

    /// <summary>
    ///     Sets how many messages the consumer handles concurrently, overriding
    ///     <see cref="ConnectionConfiguration.ConsumerDispatcherConcurrency"/> for this consumer only
    /// </summary>
    /// <remarks>For concurrency greater than one, the consumer could process messages in any order, not in the order it receives them</remarks>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="consumerDispatcherConcurrency">The concurrency to set, greater than zero</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="consumerDispatcherConcurrency"/> is zero</exception>
    /// <exception cref="NotSupportedException"><paramref name="configuration"/> was not created by EasyNetQ</exception>
    public static ISimpleConsumeConfiguration WithConsumerDispatcherConcurrency(
        this ISimpleConsumeConfiguration configuration, ushort consumerDispatcherConcurrency
    ) => SetConsumerDispatcherConcurrency(configuration, consumerDispatcherConcurrency);

    /// <summary>
    ///     Sets how many messages the subscription handles concurrently, overriding
    ///     <see cref="ConnectionConfiguration.ConsumerDispatcherConcurrency"/> for this subscription only
    /// </summary>
    /// <remarks>For concurrency greater than one, the subscription could process messages in any order, not in the order it receives them</remarks>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="consumerDispatcherConcurrency">The concurrency to set, greater than zero</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="consumerDispatcherConcurrency"/> is zero</exception>
    /// <exception cref="NotSupportedException"><paramref name="configuration"/> was not created by EasyNetQ</exception>
    public static ISubscriptionConfiguration WithConsumerDispatcherConcurrency(
        this ISubscriptionConfiguration configuration, ushort consumerDispatcherConcurrency
    ) => SetConsumerDispatcherConcurrency(configuration, consumerDispatcherConcurrency);

    /// <summary>
    ///     Sets how many messages the receiver handles concurrently, overriding
    ///     <see cref="ConnectionConfiguration.ConsumerDispatcherConcurrency"/> for this receiver only
    /// </summary>
    /// <remarks>For concurrency greater than one, the receiver could process messages in any order, not in the order it receives them</remarks>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="consumerDispatcherConcurrency">The concurrency to set, greater than zero</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="consumerDispatcherConcurrency"/> is zero</exception>
    /// <exception cref="NotSupportedException"><paramref name="configuration"/> was not created by EasyNetQ</exception>
    public static IReceiveConfiguration WithConsumerDispatcherConcurrency(
        this IReceiveConfiguration configuration, ushort consumerDispatcherConcurrency
    ) => SetConsumerDispatcherConcurrency(configuration, consumerDispatcherConcurrency);

    /// <summary>
    ///     Sets how many requests the responder handles concurrently, overriding
    ///     <see cref="ConnectionConfiguration.ConsumerDispatcherConcurrency"/> for this responder only
    /// </summary>
    /// <remarks>For concurrency greater than one, the responder could process requests in any order, not in the order it receives them</remarks>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="consumerDispatcherConcurrency">The concurrency to set, greater than zero</param>
    /// <returns>The same <paramref name="configuration"/></returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="consumerDispatcherConcurrency"/> is zero</exception>
    /// <exception cref="NotSupportedException"><paramref name="configuration"/> was not created by EasyNetQ</exception>
    public static IResponderConfiguration WithConsumerDispatcherConcurrency(
        this IResponderConfiguration configuration, ushort consumerDispatcherConcurrency
    ) => SetConsumerDispatcherConcurrency(configuration, consumerDispatcherConcurrency);

    private static TConfiguration SetConsumerDispatcherConcurrency<TConfiguration>(
        TConfiguration configuration, ushort consumerDispatcherConcurrency
    ) where TConfiguration : class
    {
        // RabbitMQ.Client would start no dispatch worker at all for zero, so the consumer would never handle a message
        if (consumerDispatcherConcurrency == 0)
            throw new ArgumentOutOfRangeException(
                nameof(consumerDispatcherConcurrency), consumerDispatcherConcurrency, "Consumer dispatcher concurrency must be greater than zero"
            );

        if (configuration is not IConsumerDispatcherConcurrencyConfiguration concurrencyConfiguration)
            throw new NotSupportedException(
                $"{configuration.GetType()} does not support consumer dispatcher concurrency, only the configurations created by EasyNetQ do"
            );

        concurrencyConfiguration.ConsumerDispatcherConcurrency = consumerDispatcherConcurrency;
        return configuration;
    }
}

/// <summary>
///     Implemented by EasyNetQ's consumer configurations to keep the value set by <see cref="ConsumerDispatcherConcurrencyExtensions"/>.
///     It is not a member of the public configuration interfaces, so that adding it does not break their implementers.
/// </summary>
internal interface IConsumerDispatcherConcurrencyConfiguration
{
    ushort? ConsumerDispatcherConcurrency { get; set; }
}
