namespace EasyNetQ;

/// <summary>
///     An abstraction for pub-sub pattern
/// </summary>
public interface IPubSub
{
    /// <summary>
    /// Publishes a message.
    /// When used with publisher confirms the task completes when the publish is confirmed.
    /// Task will throw an exception if the confirm is NACK'd or times out.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="configure">
    /// Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
    /// </param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns></returns>
    Task PublishAsync<T>(
        T message,
        Action<IPublishConfiguration> configure,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Publishes a message.
    /// When used with publisher confirms the task completes when the publish is confirmed.
    /// Task will throw an exception if the confirm is NACK'd or times out.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <typeparam name="TData">Type of data to pass to the configuration callback</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="data">Data to pass to the configuration callback</param>
    /// <param name="configure">
    /// Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
    /// </param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns></returns>
    Task PublishAsync<T, TData>(
        T message,
        TData data,
        Action<IPublishConfiguration, TData> configure,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Subscribes to a stream of messages that match a .NET type.
    /// </summary>
    /// <typeparam name="T">The type to subscribe to</typeparam>
    /// <param name="subscriptionId">
    /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.
    /// </param>
    /// <param name="onMessage">
    /// The action to run when a message arrives. onMessage can immediately return a Task and
    /// then continue processing asynchronously. When the Task completes the message will be
    /// Ack'd.
    /// </param>
    /// <param name="configure">
    /// Fluent configuration e.g. x => x.WithTopic("uk.london").WithArgument("x-message-ttl", "60")
    /// </param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>
    /// An <see cref="SubscriptionResult"/>
    /// Call Dispose on it to cancel the subscription.
    /// </returns>
    Task<SubscriptionResult> SubscribeAsync<T>(
        string subscriptionId,
        Func<T, CancellationToken, Task> onMessage,
        Action<ISubscriptionConfiguration> configure,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Subscribes to a stream of messages that match a .NET type.
    /// </summary>
    /// <typeparam name="T">The type to subscribe to</typeparam>
    /// <typeparam name="TData">Type of data to pass to the configuration callback</typeparam>
    /// <param name="subscriptionId">
    /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
    /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
    /// to load balance a subscription in a round-robin fashion.
    /// </param>
    /// <param name="onMessage">
    /// The action to run when a message arrives. onMessage can immediately return a Task and
    /// then continue processing asynchronously. When the Task completes the message will be
    /// Ack'd.
    /// </param>
    /// <param name="configureData">
    /// Data to pass to the configuration callback.
    /// </param>
    /// <param name="configure">
    /// Fluent configuration e.g. x => x.WithTopic("uk.london").WithArgument("x-message-ttl", "60")
    /// </param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>
    /// An <see cref="SubscriptionResult"/>
    /// Call Dispose on it to cancel the subscription.
    /// </returns>
    Task<SubscriptionResult> SubscribeAsync<T, TData>(
        string subscriptionId,
        Func<T, CancellationToken, Task> onMessage,
        TData configureData,
        Action<ISubscriptionConfiguration, TData> configure,
        CancellationToken cancellationToken
    );
}
