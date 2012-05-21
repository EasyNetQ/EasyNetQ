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
        /// <param name="timeToRespond">The time at which the message should be sent (UTC)</param>
        /// <param name="message">The message to response with</param>
        public static void FuturePublish<T>(this IPublishChannel publishChannel, DateTime timeToRespond, T message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            var rabbitPublishChannel = publishChannel as RabbitPublishChannel;
            if (rabbitPublishChannel == null)
            {
                throw new EasyNetQException("FuturePublish only works with a RabbitPublishChannel");
            }
            var advancedBus = rabbitPublishChannel.AdvancedBus as RabbitAdvancedBus;
            if (advancedBus == null)
            {
                throw new EasyNetQException("FuturePublish only works with a RabbitAdvancedBus");
            }
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