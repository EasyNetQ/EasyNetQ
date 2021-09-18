using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for ISendReceive
    /// </summary>
    public static class SendReceiveExtensions
    {
        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asynchronous function that handles the message</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static AwaitableDisposable<IDisposable> ReceiveAsync<T>(
            this ISendReceive sendReceive,
            string queue,
            Func<T, CancellationToken, Task> onMessage,
            Action<IReceiveConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.ReceiveAsync(queue, c => c.Add(onMessage), configure, cancellationToken);
        }

        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task SendAsync<T>(
            this ISendReceive sendReceive,
            string queue,
            T message,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.SendAsync(queue, message, _ => { }, cancellationToken);
        }

        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Send<T>(
            this ISendReceive sendReceive,
            string queue,
            T message,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            sendReceive.Send(queue, message, _ => { }, cancellationToken);
        }

        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        /// <param name="configure">
        ///     Fluent configuration e.g. x => x.WithPriority(2)
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Send<T>(
            this ISendReceive sendReceive,
            string queue,
            T message,
            Action<ISendConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            sendReceive.SendAsync(queue, message, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The synchronous function that handles the message</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static AwaitableDisposable<IDisposable> ReceiveAsync<T>(
            this ISendReceive sendReceive,
            string queue,
            Action<T> onMessage,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.ReceiveAsync(
                queue,
                onMessage,
                _ => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The synchronous function that handles the message</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static AwaitableDisposable<IDisposable> ReceiveAsync<T>(
            this ISendReceive sendReceive,
            string queue,
            Action<T> onMessage,
            Action<IReceiveConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            var onMessageAsync = TaskHelpers.FromAction<T>((m, _) => onMessage(m));

            return sendReceive.ReceiveAsync(
                queue,
                onMessageAsync,
                configure,
                cancellationToken
            );
        }

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asynchronous function that handles the message</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static AwaitableDisposable<IDisposable> ReceiveAsync<T>(
            this ISendReceive sendReceive,
            string queue,
            Func<T, Task> onMessage,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.ReceiveAsync<T>(
                queue,
                (m, _) => onMessage(m),
                _ => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Receive a message from the specified queue. Dispatch them to the given handlers
        /// </summary>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static AwaitableDisposable<IDisposable> ReceiveAsync(
            this ISendReceive sendReceive,
            string queue,
            Action<IReceiveRegistration> addHandlers,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.ReceiveAsync(
                queue,
                addHandlers,
                _ => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The synchronous function that handles the message</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static IDisposable Receive<T>(
            this ISendReceive sendReceive,
            string queue,
            Action<T> onMessage,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.Receive(queue, onMessage, _ => { }, cancellationToken);
        }

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The synchronous function that handles the message</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static IDisposable Receive<T>(
            this ISendReceive sendReceive,
            string queue,
            Action<T> onMessage,
            Action<IReceiveConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            var onMessageAsync = TaskHelpers.FromAction<T>((m, _) => onMessage(m));

            return sendReceive.Receive(
                queue,
                onMessageAsync,
                configure,
                cancellationToken
            );
        }

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asynchronous function that handles the message</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static IDisposable Receive<T>(
            this ISendReceive sendReceive,
            string queue,
            Func<T, Task> onMessage,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.Receive<T>(
                queue,
                (m, _) => onMessage(m),
                _ => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asynchronous function that handles the message</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static IDisposable Receive<T>(
            this ISendReceive sendReceive,
            string queue,
            Func<T, CancellationToken, Task> onMessage,
            Action<IReceiveConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.ReceiveAsync(
                queue,
                onMessage,
                configure,
                cancellationToken
            ).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Receive a message from the specified queue. Dispatch them to the given handlers
        /// </summary>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static IDisposable Receive(
            this ISendReceive sendReceive,
            string queue,
            Action<IReceiveRegistration> addHandlers,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.Receive(
                queue,
                addHandlers,
                _ => { },
                cancellationToken
            );
        }

        /// <summary>
        /// Receive a message from the specified queue. Dispatch them to the given handlers
        /// </summary>
        /// <param name="sendReceive">The sendReceive instance</param>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        public static IDisposable Receive(
            this ISendReceive sendReceive,
            string queue,
            Action<IReceiveRegistration> addHandlers,
            Action<IReceiveConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(sendReceive, nameof(sendReceive));

            return sendReceive.ReceiveAsync(
                queue,
                addHandlers,
                configure,
                cancellationToken
            ).GetAwaiter().GetResult();
        }
    }
}
