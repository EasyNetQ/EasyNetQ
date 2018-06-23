using System;
using System.Threading.Tasks;

namespace EasyNetQ.Scheduling
{
    public static class BusExtensions
    {
        private static IScheduler Scheduler(this IBus bus)
        {
            return bus.Advanced.Container.Resolve<IScheduler>();
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        public static void FuturePublish<T>(this IBus bus, DateTime futurePublishDate, T message) where T : class
        {
            bus.Scheduler().FuturePublishAsync(futurePublishDate, message).GetAwaiter().GetResult();;
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        public static void FuturePublish<T>(this IBus bus, DateTime futurePublishDate, T message, string topic) where T : class
        {
            bus.Scheduler().FuturePublishAsync(futurePublishDate, message, topic: topic).GetAwaiter().GetResult();;
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        /// <param name="message">The message to response with</param>
        public static void FuturePublish<T>(this IBus bus, DateTime futurePublishDate, string cancellationKey, T message) where T : class
        {
            bus.Scheduler().FuturePublishAsync(futurePublishDate, message, cancellationKey).GetAwaiter().GetResult();;
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        public static void FuturePublish<T>(this IBus bus, DateTime futurePublishDate, string topic, string cancellationKey, T message) where T : class
        {
            bus.Scheduler().FuturePublishAsync(futurePublishDate, message, topic, cancellationKey).GetAwaiter().GetResult();;
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future, using bare RabbitMQ's capabilities (message time-to-live and dead letter exchange).
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        public static void FuturePublish<T>(this IBus bus, TimeSpan messageDelay, T message) where T : class
        {
            bus.Scheduler().FuturePublishAsync(messageDelay, message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future, using bare RabbitMQ's capabilities (message time-to-live and dead letter exchange).
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        public static void FuturePublish<T>(this IBus bus, TimeSpan messageDelay, T message, string topic) where T : class
        {
            bus.Scheduler().FuturePublishAsync(messageDelay, message, topic:topic).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Unschedule all messages matching the cancellationKey.
        /// </summary>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="cancellationKey">The identifier that was used when originally scheduling the message with FuturePublish</param>
        public static void CancelFuturePublish(this IBus bus, string cancellationKey)
        {
            bus.Scheduler().CancelFuturePublishAsync(cancellationKey).GetAwaiter().GetResult();;
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        public static Task FuturePublishAsync<T>(this IBus bus, DateTime futurePublishDate, T message) where T : class
        {
            return bus.Scheduler().FuturePublishAsync(futurePublishDate, message);
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        public static Task FuturePublishAsync<T>(this IBus bus, DateTime futurePublishDate, T message, string topic) where T : class
        {
            return bus.Scheduler().FuturePublishAsync(futurePublishDate, message, topic:topic);
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        /// <param name="message">The message to response with</param>
        public static Task FuturePublishAsync<T>(this IBus bus, DateTime futurePublishDate, string cancellationKey, T message) where T : class
        {
            return bus.Scheduler().FuturePublishAsync(futurePublishDate, message, cancellationKey);
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// This required the EasyNetQ.Scheduler service to be running.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="futurePublishDate">The time at which the message should be sent (UTC)</param>
        /// <param name="cancellationKey">An identifier that can be used with CancelFuturePublish to cancel the sending of this message at a later time</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        public static Task FuturePublishAsync<T>(this IBus bus, DateTime futurePublishDate, string cancellationKey, T message, string topic) where T : class
        {
            return bus.Scheduler().FuturePublishAsync(futurePublishDate, message, topic, cancellationKey);
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future, using bare RabbitMQ's capabilities (message time-to-live and dead letter exchange).
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        public static Task FuturePublishAsync<T>(this IBus bus, TimeSpan messageDelay, T message) where T : class
        {
            return bus.Scheduler().FuturePublishAsync(messageDelay, message);
        }

        /// <summary>
        /// Schedule a message to be published at some time in the future, using bare RabbitMQ's capabilities (message time-to-live and dead letter exchange).
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        /// <param name="topic">The topic string</param>
        public static Task FuturePublishAsync<T>(this IBus bus, TimeSpan messageDelay, T message, string topic) where T : class
        {
            return bus.Scheduler().FuturePublishAsync(messageDelay, message, topic:topic);
        }

        /// <summary>
        /// Unschedule all messages matching the cancellationKey.
        /// </summary>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="cancellationKey">The identifier that was used when originally scheduling the message with FuturePublish</param>
        public static Task CancelFuturePublishAsync(this IBus bus, string cancellationKey)
        {
            return bus.Scheduler().CancelFuturePublishAsync(cancellationKey);
        }
    }
}