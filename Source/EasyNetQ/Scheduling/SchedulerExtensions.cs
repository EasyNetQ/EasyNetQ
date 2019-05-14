using System;
using System.Threading;
using EasyNetQ.Scheduling;

namespace EasyNetQ
{
    public static class SchedulerExtensions
    {
        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="scheduler">The scheduler instance</param>
        /// <param name="delay">The delay for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void FuturePublish<T>(
            this IScheduler scheduler,
            T message, 
            TimeSpan delay,
            string topic = null, 
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(scheduler, "scheduler");
            
            scheduler.FuturePublishAsync(message, delay, topic, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
    }
}
