using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    using NonGenericPublishDelegate = Func<IPubSub, object, Type, Action<IPublishConfiguration>, CancellationToken, Task>;
    using NonGenericSubscribeDelegate = Func<IPubSub, string, Type, Func<object, Type, CancellationToken, Task>, Action<ISubscriptionConfiguration>, CancellationToken, AwaitableDisposable<SubscriptionResult>>;

    /// <summary>
    ///     Various non-generic extensions for <see cref="IPubSub"/>
    /// </summary>
    public static class NonGenericPubSubExtensions
    {
        private static readonly ConcurrentDictionary<Type, NonGenericPublishDelegate> PublishDelegates = new();
        private static readonly ConcurrentDictionary<Type, NonGenericSubscribeDelegate> SubscribeDelegates = new();

        /// <summary>
        /// Publishes a message with a topic.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="messageType">The message type</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        public static Task PublishAsync(this IPubSub pubSub, object message, Type messageType, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(pubSub, nameof(pubSub));

            return pubSub.PublishAsync(message, messageType, _ => { }, cancellationToken);
        }

        /// <summary>
        /// Publishes a message with a topic.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="messageType">The message type</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        public static Task PublishAsync(
            this IPubSub pubSub,
            object message,
            Type messageType,
            string topic,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, nameof(pubSub));
            Preconditions.CheckNotNull(topic, nameof(topic));

            return pubSub.PublishAsync(message, messageType, c => c.WithTopic(topic), cancellationToken);
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="messageType">The message type</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task PublishAsync(
            this IPubSub pubSub,
            object message,
            Type messageType,
            Action<IPublishConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, nameof(pubSub));

            var publishDelegate = PublishDelegates.GetOrAdd(messageType, t =>
            {
                var publishMethodInfo = typeof(IPubSub).GetMethod("PublishAsync");
                if (publishMethodInfo == null)
                    throw new MissingMethodException(nameof(IPubSub), "PublishAsync");

                var genericPublishMethodInfo = publishMethodInfo.MakeGenericMethod(t);
                var pubSubParameter = Expression.Parameter(typeof(IPubSub), "pubSub");
                var messageParameter = Expression.Parameter(typeof(object), "message");
                var messageTypeParameter = Expression.Parameter(typeof(Type), "messageType");
                var configureParameter = Expression.Parameter(typeof(Action<IPublishConfiguration>), "configure");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                var genericPublishMethodCallExpression = Expression.Call(
                    pubSubParameter,
                    genericPublishMethodInfo,
                    Expression.Convert(messageParameter, t),
                    configureParameter,
                    cancellationTokenParameter
                );
                var lambda = Expression.Lambda<NonGenericPublishDelegate>(
                    genericPublishMethodCallExpression,
                    pubSubParameter,
                    messageParameter,
                    messageTypeParameter,
                    configureParameter,
                    cancellationTokenParameter
                );
                return lambda.Compile();
            });
            return publishDelegate(pubSub, message, messageType, configure, cancellationToken);
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="messageType">The message type</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish(this IPubSub pubSub, object message, Type messageType, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(pubSub, nameof(pubSub));

            pubSub.Publish(message, messageType, _ => { }, cancellationToken);
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="messageType">The message type</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish(
            this IPubSub pubSub,
            object message,
            Type messageType,
            Action<IPublishConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, nameof(pubSub));

            pubSub.PublishAsync(message, messageType, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Publishes a message with a topic
        /// </summary>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="messageType">The message type</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish(
            this IPubSub pubSub,
            object message,
            Type messageType,
            string topic,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, nameof(pubSub));

            pubSub.Publish(message, messageType, c => c.WithTopic(topic), cancellationToken);
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="messageType">
        /// The type to subscribe to
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
        /// Call Dispose on it or on its <see cref="SubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static AwaitableDisposable<SubscriptionResult> SubscribeAsync(
            this IPubSub pubSub,
            string subscriptionId,
            Type messageType,
            Func<object, Type, CancellationToken, Task> onMessage,
            Action<ISubscriptionConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            var subscribeDelegate = SubscribeDelegates.GetOrAdd(messageType, t =>
            {
                var subscribeMethodInfo = typeof(IPubSub).GetMethod("SubscribeAsync");
                if (subscribeMethodInfo == null)
                    throw new MissingMethodException(nameof(IPubSub), "SubscribeAsync");

                var genericSubscribeMethodInfo = subscribeMethodInfo.MakeGenericMethod(t);
                var pubSubParameter = Expression.Parameter(typeof(IPubSub), "pubSub");
                var subscriptionIdParameter = Expression.Parameter(typeof(string), "subscriptionId");
                var messageTypeParameter = Expression.Parameter(typeof(Type), "messageType");
                var messageParameter = Expression.Parameter(t, "message");
                var onMessageParameter = Expression.Parameter(typeof(Func<object, Type, CancellationToken, Task>), "onMessage");
                var configureParameter = Expression.Parameter(typeof(Action<ISubscriptionConfiguration>), "configure");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                var onMessageInvocationExpression = Expression.Lambda(
                    Expression.GetFuncType(t, typeof(CancellationToken), typeof(Task)),
                    Expression.Invoke(
                        onMessageParameter,
                        Expression.Convert(messageParameter, typeof(object)),
                        Expression.Call(
                            Expression.Convert(messageParameter, typeof(object)),
                            typeof(object).GetMethod("GetType", Array.Empty<Type>()) ?? throw new InvalidOperationException()
                        ),
                        cancellationTokenParameter
                    ),
                    messageParameter,
                    cancellationTokenParameter
                );
                var lambda = Expression.Lambda<NonGenericSubscribeDelegate>(
                    Expression.Call(
                        pubSubParameter,
                        genericSubscribeMethodInfo,
                        subscriptionIdParameter,
                        onMessageInvocationExpression,
                        configureParameter,
                        cancellationTokenParameter
                    ),
                    pubSubParameter,
                    subscriptionIdParameter,
                    messageTypeParameter,
                    onMessageParameter,
                    configureParameter,
                    cancellationTokenParameter
                );
                return lambda.Compile();
            });
            return subscribeDelegate(pubSub, subscriptionId, messageType, onMessage, configure, cancellationToken);
        }

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <param name="pubSub">The pubSub instance</param>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="messageType">
        /// The type to subscribe to
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
        /// Call Dispose on it or on its <see cref="SubscriptionResult.ConsumerCancellation"/> to cancel the subscription.
        /// </returns>
        public static SubscriptionResult Subscribe(
            this IPubSub pubSub,
            string subscriptionId,
            Type messageType,
            Func<object, Type, CancellationToken, Task> onMessage,
            Action<ISubscriptionConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(pubSub, "pubSub");

            return pubSub.SubscribeAsync(subscriptionId, messageType, onMessage, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
    }
}
