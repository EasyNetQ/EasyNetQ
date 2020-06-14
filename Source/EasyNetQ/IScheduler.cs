using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    /// <summary>
    /// Provides a simple Publish API to schedule a message to be published at some time in the future.
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to response with</param>
        /// <param name="delay">The delay for message to publish in future</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationToken">The cancellation token</param>
        Task FuturePublishAsync<T>(
            T message,
            TimeSpan delay,
            string topic,
            CancellationToken cancellationToken = default
        );
    }
}
