using System;
using System.Threading.Tasks;

namespace EasyNetQ.Scheduling
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
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class;

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string topic, string cancellationKey = null) where T : class;

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        void FuturePublish<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class;

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        void FuturePublish<T>(DateTime futurePublishDate, T message, string topic, string cancellationKey = null) where T : class;


        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class;

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null) where T : class;


        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        void FuturePublish<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class;

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        void FuturePublish<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null) where T : class;
        /// <summary>
        /// Unschedule all messages matching the cancellationKey.
        /// </summary>
        /// <param name="cancellationKey">The identifier that was used when originally scheduling the message with FuturePublish</param>
        Task CancelFuturePublishAsync(string cancellationKey);


        /// <summary>
        /// Unschedule all messages matching the cancellationKey.
        /// </summary>
        /// <param name="cancellationKey">The identifier that was used when originally scheduling the message with FuturePublish</param>
        void CancelFuturePublish(string cancellationKey);
    }
}
