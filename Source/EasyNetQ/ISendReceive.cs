using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     An abstraction for send-receive pattern
    /// </summary>
    public interface ISendReceive
    {
        /// <summary>
        /// Send a message to the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="queue">The queue to send the message to</param>
        /// <param name="message">The message to send</param>
        /// <param name="configure">
        ///     Fluent configuration e.g. x => x.WithPriority(2)
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task SendAsync<T>(
            string queue,
            T message,
            Action<ISendConfiguration> configure,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asynchronous function that handles the message</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        AwaitableDisposable<IDisposable> ReceiveAsync<T>(
            string queue,
            Func<T, CancellationToken, Task> onMessage,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Receive a message from the specified queue. Dispatch them to the given handlers
        /// </summary>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        AwaitableDisposable<IDisposable> ReceiveAsync(
            string queue,
            Action<IReceiveRegistration> addHandlers,
            Action<IConsumerConfiguration> configure,
            CancellationToken cancellationToken = default
        );
    }
}
