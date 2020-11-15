using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    using NonGenericSendDelegate = Func<ISendReceive, string, object, Type, Action<ISendConfiguration>, CancellationToken, Task>;


    /// <summary>
    ///     Various extensions for ISendReceive
    /// </summary>
    public static class NonGenericSendReceiveExtensions
    {
        private static readonly ConcurrentDictionary<Type, NonGenericSendDelegate> SendDelegates
            = new ConcurrentDictionary<Type, NonGenericSendDelegate>();


        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task SendAsync(
            this ISendReceive sendReceive,
            string queue,
            object message,
            Type messageType,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, "sendReceive");

            return sendReceive.SendAsync(queue, message, messageType, c => { }, cancellationToken);
        }

        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <param name="configure">
        ///     Fluent configuration e.g. x => x.WithPriority(2)
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task SendAsync(
            this ISendReceive sendReceive,
            string queue,
            object message,
            Type messageType,
            Action<ISendConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, "sendReceive");

            var sendDelegate = SendDelegates.GetOrAdd(messageType, t =>
            {
                var sendMethodInfo = typeof(ISendReceive).GetMethod("SendAsync");
                if (sendMethodInfo == null)
                    throw new MissingMethodException(nameof(ISendReceive), "SendAsync");

                var genericSendMethodInfo = sendMethodInfo.MakeGenericMethod(t);
                var sendReceiveParameter = Expression.Parameter(typeof(ISendReceive), "sendReceive");
                var queueParameter = Expression.Parameter(typeof(string), "queue");
                var messageParameter = Expression.Parameter(typeof(object), "message");
                var messageTypeParameter = Expression.Parameter(typeof(Type), "messageType");
                var configureParameter = Expression.Parameter(typeof(Action<ISendConfiguration>), "configure");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                var genericSendMethodCallExpression = Expression.Call(
                    sendReceiveParameter,
                    genericSendMethodInfo,
                    queueParameter,
                    Expression.Convert(messageParameter, t),
                    configureParameter,
                    cancellationTokenParameter
                );
                var lambda = Expression.Lambda<NonGenericSendDelegate>(
                    genericSendMethodCallExpression,
                    sendReceiveParameter,
                    queueParameter,
                    messageParameter,
                    messageTypeParameter,
                    configureParameter,
                    cancellationTokenParameter
                );
                return lambda.Compile();
            });
            return sendDelegate(sendReceive, queue, message, messageType, configure, cancellationToken);
        }

        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Send(
            this ISendReceive sendReceive,
            string queue,
            object message,
            Type messageType,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, "sendReceive");

            sendReceive.Send(queue, message, messageType, c => { }, cancellationToken);
        }

        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        /// <param name="messageType">The message type</param>
        /// <param name="configure">
        ///     Fluent configuration e.g. x => x.WithPriority(2)
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Send(
            this ISendReceive sendReceive,
            string queue,
            object message,
            Type messageType,
            Action<ISendConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, "sendReceive");

            sendReceive.SendAsync(queue, message, messageType, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
    }
}
