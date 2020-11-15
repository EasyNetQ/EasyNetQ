using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    using NonGenericPublishDelegate = Func<IPubSub, object, Type, Action<IPublishConfiguration>, CancellationToken, Task>;

    /// <summary>
    ///     Various extensions for IPubSub
    /// </summary>
    public static class NonGenericPubSubExtensions
    {
        private static readonly ConcurrentDictionary<Type, NonGenericPublishDelegate> PublishDelegates = new ConcurrentDictionary<Type, NonGenericPublishDelegate>();

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
            Preconditions.CheckNotNull(pubSub, "pubSub");

            return pubSub.PublishAsync(message, messageType, c => { }, cancellationToken);
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
            Preconditions.CheckNotNull(pubSub, "pubSub");
            Preconditions.CheckNotNull(topic, "topic");

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
            Preconditions.CheckNotNull(pubSub, "pubSub");

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
            Preconditions.CheckNotNull(pubSub, "pubSub");

            pubSub.Publish(message, messageType, c => { }, cancellationToken);
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
            Preconditions.CheckNotNull(pubSub, "pubSub");

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
            Preconditions.CheckNotNull(pubSub, "pubSub");

            pubSub.Publish(message, messageType, c => c.WithTopic(topic), cancellationToken);
        }
    }
}
