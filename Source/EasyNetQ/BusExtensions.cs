using System;
using System.Threading.Tasks;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public static class BusExtensions
    {
        public static void FuturePublish<T>(this IBus bus, TimeSpan messageOffset, T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            var advancedBus = bus.Advanced;
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var connectionConfiguration = advancedBus.Container.Resolve<IConnectionConfiguration>();
            var offset = Round(messageOffset);
            var offsetString = offset.ToString(@"hh\_mm\_ss");
            var exchangeName = conventions.ExchangeNamingConvention(typeof (T));
            var futureExchangeName = exchangeName + "_" + offsetString;
            var futureQueueName = conventions.QueueNamingConvention(typeof (T), offsetString);
            var futureExchange = advancedBus.ExchangeDeclare(futureExchangeName, ExchangeType.Topic);
            var futureQueue = advancedBus.QueueDeclare(futureQueueName, perQueueTtl: (int) offset.TotalMilliseconds, deadLetterExchange: exchangeName);
            advancedBus.Bind(futureExchange, futureQueue, "#");
            var easyNetQMessage = new Message<T>(message)
                {
                    Properties =
                        {
                            DeliveryMode = (byte) (connectionConfiguration.PersistentMessages ? 2 : 1)
                        }
                };

            bus.Advanced.Publish(futureExchange, "#", false, false, easyNetQMessage);
        }

        private static TimeSpan Round(TimeSpan timeSpan)
        {
            return new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, 0);
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
            FuturePublish(bus, futurePublishDate, null, message);
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
            Preconditions.CheckNotNull(message, "message");

            var advancedBus = bus.Advanced;

            var typeNameSerializer = advancedBus.Container.Resolve<ITypeNameSerializer>();
            var serializer = advancedBus.Container.Resolve<ISerializer>();

            var typeName = typeNameSerializer.Serialize(typeof (T));
            var messageBody = serializer.MessageToBytes(message);

            bus.Publish(new ScheduleMe
                {
                    WakeTime = futurePublishDate,
                    BindingKey = typeName,
                    CancellationKey = cancellationKey,
                    InnerMessage = messageBody
                });
        }

        /// <summary>
        /// Unschedule all messages matching the cancellationKey.
        /// </summary>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="cancellationKey">The identifier that was used when originally scheduling the message with FuturePublish</param>
        public static void CancelFuturePublish(this IBus bus, string cancellationKey)
        {
            bus.Publish(new UnscheduleMe
                {
                    CancellationKey = cancellationKey
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
            return FuturePublishAsync(bus, futurePublishDate, null, message);
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
            Preconditions.CheckNotNull(message, "message");

            var advancedBus = bus.Advanced;

            var typeNameSerializer = advancedBus.Container.Resolve<ITypeNameSerializer>();
            var serializer = advancedBus.Container.Resolve<ISerializer>();

            var typeName = typeNameSerializer.Serialize(typeof (T));
            var messageBody = serializer.MessageToBytes(message);

            return bus.PublishAsync(new ScheduleMe
                {
                    WakeTime = futurePublishDate,
                    BindingKey = typeName,
                    CancellationKey = cancellationKey,
                    InnerMessage = messageBody
                });
        }

        /// <summary>
        /// Unschedule all messages matching the cancellationKey.
        /// </summary>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="cancellationKey">The identifier that was used when originally scheduling the message with FuturePublish</param>
        public static Task CancelFuturePublishAsync(this IBus bus, string cancellationKey)
        {
            return bus.PublishAsync(new UnscheduleMe
                {
                    CancellationKey = cancellationKey
                });
        }
    }
}