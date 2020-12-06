using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    using NonGenericFuturePublishDelegate = Func<IScheduler, object, Type, TimeSpan, Action<IFuturePublishConfiguration>, CancellationToken, Task>;

    /// <summary>
    ///     Various extensions for IScheduler
    /// </summary>
    public static class NonGenericSchedulerExtensions
    {
        private static readonly ConcurrentDictionary<Type, NonGenericFuturePublishDelegate> FuturePublishDelegates
            = new ConcurrentDictionary<Type, NonGenericFuturePublishDelegate>();

        /// <summary>
        /// Schedule a message to be published at some time in the future
        /// </summary>
        /// <param name="scheduler">The scheduler instance</param>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <param name="delay">The delay for message to publish in future</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task FuturePublishAsync(
            this IScheduler scheduler,
            object message,
            Type messageType,
            TimeSpan delay,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(scheduler, "scheduler");

            return scheduler.FuturePublishAsync(message, messageType, delay, c => { }, cancellationToken);
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future
        /// </summary>
        /// <param name="scheduler">The scheduler instance</param>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <param name="delay">The delay for message to publish in future</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void FuturePublish(
            this IScheduler scheduler,
            object message,
            Type messageType,
            TimeSpan delay,
            string topic,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(scheduler, "scheduler");

            scheduler.FuturePublishAsync(message, messageType, delay, c => c.WithTopic(topic), cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future
        /// </summary>
        /// <param name="scheduler">The scheduler instance</param>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <param name="delay">The delay for message to publish in future</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void FuturePublish(
            this IScheduler scheduler,
            object message,
            Type messageType,
            TimeSpan delay,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(scheduler, "scheduler");

            scheduler.FuturePublishAsync(message, messageType, delay, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

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
        public static void FuturePublish(
            this IScheduler scheduler,
            object message,
            Type messageType,
            TimeSpan delay,
            Action<IFuturePublishConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(scheduler, "scheduler");

            scheduler.FuturePublishAsync(message, messageType, delay, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

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
        public static Task FuturePublishAsync(
            this IScheduler scheduler,
            object message,
            Type messageType,
            TimeSpan delay,
            Action<IFuturePublishConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(scheduler, "scheduler");

            var futurePublishDelegate = FuturePublishDelegates.GetOrAdd(messageType, t =>
            {
                var futurePublishMethodInfo = typeof(IScheduler).GetMethod("FuturePublishAsync");
                if (futurePublishMethodInfo == null)
                    throw new MissingMethodException(nameof(IScheduler), "FuturePublishAsync");

                var genericFuturePublishMethodInfo = futurePublishMethodInfo.MakeGenericMethod(t);
                var schedulerParameter = Expression.Parameter(typeof(IScheduler), "scheduler");
                var messageParameter = Expression.Parameter(typeof(object), "message");
                var messageTypeParameter = Expression.Parameter(typeof(Type), "messageType");
                var delayParameter = Expression.Parameter(typeof(TimeSpan), "delay");
                var configureParameter = Expression.Parameter(typeof(Action<IFuturePublishConfiguration>), "configure");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                var genericFuturePublishMethodCallExpression = Expression.Call(
                    schedulerParameter,
                    genericFuturePublishMethodInfo,
                    Expression.Convert(messageParameter, t),
                    delayParameter,
                    configureParameter,
                    cancellationTokenParameter
                );
                var lambda = Expression.Lambda<NonGenericFuturePublishDelegate>(
                    genericFuturePublishMethodCallExpression,
                    schedulerParameter,
                    messageParameter,
                    messageTypeParameter,
                    delayParameter,
                    configureParameter,
                    cancellationTokenParameter
                );
                return lambda.Compile();
            });
            return futurePublishDelegate(scheduler, message, messageType, delay, configure, cancellationToken);
        }
    }
}
