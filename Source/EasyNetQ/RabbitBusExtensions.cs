using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public static class RabbitBusExtensions
    {
        /// <summary>
        /// Schedule a message to be published at some time in the future.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        public static void FuturePublish<T>(this IBus bus, TimeSpan messageDelay, T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            var advancedBus = bus.Advanced;
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var connectionConfiguration = advancedBus.Container.Resolve<IConnectionConfiguration>();
            var delay = Round(messageDelay);
            var delayString = delay.ToString(@"hh\_mm\_ss");
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var futureExchangeName = exchangeName + "_" + delayString;
            var futureQueueName = conventions.QueueNamingConvention(typeof(T), delayString);
            var futureExchange = advancedBus.ExchangeDeclare(futureExchangeName, ExchangeType.Topic);
            var futureQueue = advancedBus.QueueDeclare(futureQueueName, perQueueTtl: (int)delay.TotalMilliseconds, deadLetterExchange: exchangeName);
            advancedBus.Bind(futureExchange, futureQueue, "#");
            var easyNetQMessage = new Message<T>(message)
                {
                    Properties =
                        {
                            DeliveryMode = (byte)(connectionConfiguration.PersistentMessages ? 2 : 1)
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
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The IBus instance to publish on</param>
        /// <param name="messageDelay">The delay time for message to publish in future</param>
        /// <param name="message">The message to response with</param>
        public static Task FuturePublishAsync<T>(this IBus bus, TimeSpan messageDelay, T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

            var advancedBus = bus.Advanced;
            var conventions = advancedBus.Container.Resolve<IConventions>();
            var connectionConfiguration = advancedBus.Container.Resolve<IConnectionConfiguration>();
            var delay = Round(messageDelay);
            var delayString = delay.ToString(@"hh\_mm\_ss");
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var futureExchangeName = exchangeName + "_" + delayString;
            var futureQueueName = conventions.QueueNamingConvention(typeof(T), delayString);
            var futureExchange = advancedBus.ExchangeDeclare(futureExchangeName, ExchangeType.Topic);
            var futureQueue = advancedBus.QueueDeclare(futureQueueName, perQueueTtl: (int)delay.TotalMilliseconds, deadLetterExchange: exchangeName);
            advancedBus.Bind(futureExchange, futureQueue, "#");
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = (byte)(connectionConfiguration.PersistentMessages ? 2 : 1)
                }
            };

            return bus.Advanced.PublishAsync(futureExchange, "#", false, false, easyNetQMessage);
        }

    }
}