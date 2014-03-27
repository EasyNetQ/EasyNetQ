﻿using System;
using System.Threading.Tasks;
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

            var typeNameSerializer = advancedBus.Container.Resolve<ITypeNameSerializer>();
            var serializer = advancedBus.Container.Resolve<ISerializer>();

            var typeName = typeNameSerializer.Serialize(typeof(T));
            var messageBody = serializer.MessageToBytes(message);

            bus.Publish(new ScheduleMe
            {
                WakeTime = futurePublishDate,
                BindingKey = typeName,
                InnerMessage = messageBody
            });
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
            Preconditions.CheckNotNull(message, "message");

            var advancedBus = bus.Advanced;

            var typeNameSerializer = advancedBus.Container.Resolve<ITypeNameSerializer>();
            var serializer = advancedBus.Container.Resolve<ISerializer>();

            var typeName = typeNameSerializer.Serialize(typeof(T));
            var messageBody = serializer.MessageToBytes(message);

            return bus.PublishAsync(new ScheduleMe
            {
                WakeTime = futurePublishDate,
                BindingKey = typeName,
                InnerMessage = messageBody
            });
        }
    }
}