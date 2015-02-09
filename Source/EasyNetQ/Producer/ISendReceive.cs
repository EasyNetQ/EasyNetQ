using System;
using System.Threading.Tasks;
using EasyNetQ.Consumer;

namespace EasyNetQ.Producer
{
    public interface ISendReceive
    {
        /// <summary>
        /// Send a message to the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="queue">The queue to send the message to</param>
        /// <param name="message">The message to send</param>
        void Send<T>(string queue, T message) where T : class;

        /// <summary>
        /// Send a message to the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="queue">The queue to send the message to</param>
        /// <param name="message">The message to send</param>
        Task SendAsync<T>(string queue, T message) where T : class;

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The synchronous function that handles the message</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        IDisposable Receive<T>(string queue, Action<T> onMessage) where T : class;

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The synchronous function that handles the message</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        IDisposable Receive<T>(string queue, Action<T> onMessage, Action<IConsumerConfiguration> configure) where T : class;

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asynchronous function that handles the message</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        IDisposable Receive<T>(string queue, Func<T, Task> onMessage) where T : class;

        /// <summary>
        /// Receive a message from the specified queue
        /// </summary>
        /// <typeparam name="T">The type of message to receive</typeparam>
        /// <param name="queue">The queue to receive from</param>
        /// <param name="onMessage">The asynchronous function that handles the message</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        IDisposable Receive<T>(string queue, Func<T, Task> onMessage, Action<IConsumerConfiguration> configure) where T : class;

        /// <summary>
        /// Receive a message from the specified queue. Dispatch them to the given handlers
        /// </summary>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers);

        /// <summary>
        /// Receive a message from the specified queue. Dispatch them to the given handlers
        /// </summary>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="addHandlers">A function to add handlers</param>
        /// <param name="configure">Action to configure consumer with</param>
        /// <returns>Consumer cancellation. Call Dispose to stop consuming</returns>
        IDisposable Receive(string queue, Action<IReceiveRegistration> addHandlers, Action<IConsumerConfiguration> configure);
    }
}