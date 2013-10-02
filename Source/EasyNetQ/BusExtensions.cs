using System;
using EasyNetQ.SystemMessages;

namespace EasyNetQ
{
    public static class BusExtensions
    {
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
            Preconditions.CheckNotNull(message, "message");

            var advancedBus = bus.Advanced;
            var typeName = advancedBus.SerializeType(typeof(T));
            var messageBody = advancedBus.Serializer.MessageToBytes(message);

            bus.Publish(new ScheduleMe
            {
                WakeTime = futurePublishDate,
                BindingKey = typeName,
                InnerMessage = messageBody
            });
        }
    }
}