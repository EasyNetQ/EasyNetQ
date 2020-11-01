using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for IPubSub
    /// </summary>
    public static class PubSubExtensions
    {
        /// <summary>
        /// Publishes a message with a topic.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        public static Task PublishAsync<T>(this IPubSub pubSub, T message, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            return pubSub.PublishAsync(message, c => { }, cancellationToken);
        }

        /// <summary>
        /// Publishes a message with a topic.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// /// <param name="message">The message to publish</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        public static Task PublishAsync<T>(this IPubSub pubSub, T message, string topic, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");
            Preconditions.CheckNotNull(topic, "topic");

            return pubSub.PublishAsync(message, c => c.WithTopic(topic), cancellationToken);
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish<T>(this IPubSub pubSub, T message, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            pubSub.Publish(message, c => { }, cancellationToken);
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish<T>(this IPubSub pubSub, T message, Action<IPublishConfiguration> configure, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            pubSub.PublishAsync(message, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Publishes a message with a topic
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish<T>(this IPubSub pubSub, T message, string topic, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            pubSub.Publish(message, c => c.WithTopic(topic), cancellationToken);
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. When onMessage completes the message
        /// receipt is Ack'd. All onMessage delegates are processed on a single thread so you should
        /// avoid long running blocking IO operations. Consider using SubscribeAsync
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static AwaitableDisposable<ISubscriptionResult> SubscribeAsync<T>(
            this IPubSub pubSub,
            string subscriptionId,
            Action<T> onMessage,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            return pubSub.SubscribeAsync(
                subscriptionId,
                onMessage,
                c => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. When onMessage completes the message
        /// receipt is Ack'd. All onMessage delegates are processed on a single thread so you should
        /// avoid long running blocking IO operations. Consider using SubscribeAsync
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("uk.london")
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static AwaitableDisposable<ISubscriptionResult> SubscribeAsync<T>(
            this IPubSub pubSub,
            string subscriptionId,
            Action<T> onMessage,
            Action<ISubscriptionConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            var onMessageAsync = TaskHelpers.FromAction<T>((m, c) => onMessage(m));

            return pubSub.SubscribeAsync(
                subscriptionId,
                onMessageAsync,
                configure,
                cancellationToken
            );
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// Allows the subscriber to complete asynchronously.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
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
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static AwaitableDisposable<ISubscriptionResult> SubscribeAsync<T>(
            this IPubSub pubSub,
            string subscriptionId,
            Func<T, Task> onMessage,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            return pubSub.SubscribeAsync<T>(
                subscriptionId,
                (m, c) => onMessage(m),
                c => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. When onMessage completes the message
        /// receipt is Ack'd. All onMessage delegates are processed on a single thread so you should
        /// avoid long running blocking IO operations. Consider using SubscribeAsync
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static ISubscriptionResult Subscribe<T>(
            this IPubSub pubSub,
            string subscriptionId,
            Action<T> onMessage,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            return pubSub.Subscribe(
                subscriptionId,
                onMessage,
                c => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. When onMessage completes the message
        /// receipt is Ack'd. All onMessage delegates are processed on a single thread so you should
        /// avoid long running blocking IO operations. Consider using SubscribeAsync
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("uk.london")
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static ISubscriptionResult Subscribe<T>(
            this IPubSub pubSub,
            string subscriptionId,
            Action<T> onMessage,
            Action<ISubscriptionConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            var onMessageAsync = TaskHelpers.FromAction<T>((m, c) => onMessage(m));

            return pubSub.Subscribe(
                subscriptionId,
                onMessageAsync,
                configure,
                cancellationToken
            );
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// Allows the subscriber to complete asynchronously.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
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
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static ISubscriptionResult Subscribe<T>(
            this IPubSub pubSub,
            string subscriptionId,
            Func<T, Task> onMessage,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            return pubSub.Subscribe<T>(
                subscriptionId,
                (m, c) => onMessage(m),
                c => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// Allows the subscriber to complete asynchronously.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="pubSub">The pubSub instance</param>
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
        /// Fluent configuration e.g. x => x.WithTopic("uk.london")
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>
        /// An <see cref="ISubscriptionResult"/>
        /// Call Dispose on it or on its <see cref="ISubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static ISubscriptionResult Subscribe<T>(
            this IPubSub pubSub,
            string subscriptionId,
            Func<T, CancellationToken, Task> onMessage,
            Action<ISubscriptionConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            return pubSub.SubscribeAsync(
                subscriptionId,
                onMessage,
                configure,
                cancellationToken
            ).GetAwaiter().GetResult();
        }
    }
}
