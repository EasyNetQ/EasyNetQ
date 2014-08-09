using System;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public class Scheduler : IScheduler
    {
        private static readonly TimeSpan MaxMessageDelay = TimeSpan.FromMilliseconds(int.MaxValue);

        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly ISerializer serializer;
        private readonly ITypeNameSerializer typeNameSerializer;

        public Scheduler(
            IAdvancedBus advancedBus,
            IConventions conventions,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            ISerializer serializer,
            ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(serializer, "serializer");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.serializer = serializer;
            this.typeNameSerializer = typeNameSerializer;
        }

        public void FuturePublish<T>(DateTime futurePublishDate, T message) where T : class
        {
            FuturePublish(futurePublishDate, null, message);
        }

        public void FuturePublish<T>(DateTime futurePublishDate, string cancellationKey, T message) where T : class
        {
            FuturePublishAsync(futurePublishDate, cancellationKey, message).Wait();
        }

        public void FuturePublish<T>(TimeSpan messageDelay, T message) where T : class
        {
            FuturePublishAsync(messageDelay, message).Wait();
        }

        public void CancelFuturePublish(string cancellationKey)
        {
            CancelFuturePublishAsync(cancellationKey).Wait();
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, T message) where T : class
        {
            return FuturePublishAsync(futurePublishDate, null, message);
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, string cancellationKey, T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            var messageType = typeof(ScheduleMe);
            return publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, messageType, ExchangeType.Topic).Then(exchange =>
            {
                var typeName = typeNameSerializer.Serialize(typeof(T));
                var messageBody = serializer.MessageToBytes(message);
                var easyNetQMessage = new Message<ScheduleMe>(new ScheduleMe
                {
                    WakeTime = futurePublishDate,
                    BindingKey = typeName,
                    CancellationKey = cancellationKey,
                    InnerMessage = messageBody
                }) { Properties = { DeliveryMode = (byte)(messageDeliveryModeStrategy.IsPersistent(messageType) ? 2 : 1) } };
                return advancedBus.PublishAsync(exchange, conventions.TopicNamingConvention(messageType), false, false, easyNetQMessage);
            });
        }

        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckLess(messageDelay, MaxMessageDelay, "messageDelay");
            var delay = Round(messageDelay);
            var delayString = delay.ToString(@"dd\_hh\_mm\_ss");
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var futureExchangeName = exchangeName + "_" + delayString;
            var futureQueueName = conventions.QueueNamingConvention(typeof(T), delayString);
            return advancedBus.ExchangeDeclareAsync(futureExchangeName, ExchangeType.Topic)
                .Then(futureExchange => advancedBus.QueueDeclareAsync(futureQueueName, perQueueTtl: (int)delay.TotalMilliseconds, deadLetterExchange: exchangeName)
                                                   .Then(futureQueue => advancedBus.BindAsync(futureExchange, futureQueue, "#"))
                                                   .Then(() =>
                                                   {
                                                       var easyNetQMessage = new Message<T>(message)
                                                       {
                                                           Properties =
                                                           {
                                                               DeliveryMode = (byte)(messageDeliveryModeStrategy.IsPersistent(typeof(T)) ? 2 : 1)
                                                           }
                                                       };
                                                       return advancedBus.PublishAsync(futureExchange, "#", false, false, easyNetQMessage);
                                                   }));
        }

        private static TimeSpan Round(TimeSpan timeSpan)
        {
            return new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, 0);
        }

        public Task CancelFuturePublishAsync(string cancellationKey)
        {
            var messageType = typeof(UnscheduleMe);
            return publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, messageType, ExchangeType.Topic).Then(exchange =>
            {
                var easyNetQMessage = new Message<UnscheduleMe>(new UnscheduleMe
                {
                    CancellationKey = cancellationKey
                }) { Properties = { DeliveryMode = (byte)(messageDeliveryModeStrategy.IsPersistent(messageType) ? 2 : 1) } };
                return advancedBus.PublishAsync(exchange, conventions.TopicNamingConvention(messageType), false, false, easyNetQMessage);
            });
        }
    }
}
