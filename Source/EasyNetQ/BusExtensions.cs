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
        /// <param name="publishChannel">The publish channel to publish the future message on</param>
        /// <param name="timeToRespond">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        public static void FuturePublish<T>(this IPublishChannel publishChannel, DateTime timeToRespond, T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            var advancedBus = publishChannel.Bus.Advanced;
            var typeName = advancedBus.SerializeType(typeof(T));
            var messageBody = advancedBus.Serializer.MessageToBytes(message);

            publishChannel.Publish(new ScheduleMe
            {
                WakeTime = timeToRespond,
                BindingKey = typeName,
                InnerMessage = messageBody
            });
        }
    }
}