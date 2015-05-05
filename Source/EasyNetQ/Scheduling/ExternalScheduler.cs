using System;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class ExternalScheduler : IScheduler
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly ISerializer serializer;
        private readonly ITypeNameSerializer typeNameSerializer;

        public ExternalScheduler(
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

        public void FuturePublish<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            FuturePublishAsync(futurePublishDate, message, cancellationKey).Wait();
        }

        public void FuturePublish<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            FuturePublishAsync(DateTime.UtcNow.Add(messageDelay), message, cancellationKey).Wait();
        }

        public void CancelFuturePublish(string cancellationKey)
        {
            CancelFuturePublishAsync(cancellationKey).Wait();
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            var messageType = typeof (ScheduleMe);
            return publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, messageType, ExchangeType.Topic).Then(exchange =>
            {
                var typeName = typeNameSerializer.Serialize(typeof (T));
                var messageBody = serializer.MessageToBytes(message);
                var scheduleMe = new ScheduleMe
                {
                    WakeTime = futurePublishDate,
                    BindingKey = typeName,
                    CancellationKey = cancellationKey,
                    InnerMessage = messageBody
                };
                var easyNetQMessage = new Message<ScheduleMe>(scheduleMe)
                {
                    Properties =
                    {
                        DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType)
                    }
                };
                return advancedBus.PublishAsync(exchange, conventions.TopicNamingConvention(messageType), false, false, easyNetQMessage);
            });
        }

        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            return FuturePublishAsync(DateTime.UtcNow.Add(messageDelay), message, cancellationKey);
        }

        public Task CancelFuturePublishAsync(string cancellationKey)
        {
            var messageType = typeof (UnscheduleMe);
            return publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, messageType, ExchangeType.Topic).Then(exchange =>
            {
                var unscheduleMe = new UnscheduleMe {CancellationKey = cancellationKey};
                var easyNetQMessage = new Message<UnscheduleMe>(unscheduleMe)
                {
                    Properties =
                    {
                        DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType)
                    }
                };
                return advancedBus.PublishAsync(exchange, conventions.TopicNamingConvention(messageType), false, false, easyNetQMessage);
            });
        }
    }
}