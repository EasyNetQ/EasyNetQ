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
        private readonly ITypeNameSerializer typeNameSerializer;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;

        public ExternalScheduler(
            IAdvancedBus advancedBus,
            IConventions conventions,
            ITypeNameSerializer typeNameSerializer,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IMessageSerializationStrategy messageSerializationStrategy)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(messageSerializationStrategy, "messageSerializationStrategy");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.typeNameSerializer = typeNameSerializer;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.messageSerializationStrategy = messageSerializationStrategy;
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            var scheduleMeType = typeof(ScheduleMe);
            return publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, scheduleMeType, ExchangeType.Topic).Then(scheduleMeExchange =>
            {
                var baseMessageType = typeof(T);
                var concreteMessageType = message.GetType();
                var serializedMessage = messageSerializationStrategy.SerializeMessage(new Message<T>(message)
                {
                    Properties =
                    {
                        DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(concreteMessageType)
                    }
                });
                var scheduleMe = new ScheduleMe
                {
                    WakeTime = futurePublishDate,
                    CancellationKey = cancellationKey,
                    InnerMessage = serializedMessage.Body,
                    MessageProperties = serializedMessage.Properties,
                    BindingKey = typeNameSerializer.Serialize(typeof (T)), 
                    ExchangeType = ExchangeType.Topic,
                    Exchange = conventions.ExchangeNamingConvention(baseMessageType),
                    RoutingKey = "#"
                };
                var easyNetQMessage = new Message<ScheduleMe>(scheduleMe)
                {
                    Properties =
                    {
                        DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(scheduleMeType)
                    }
                };
                return advancedBus.PublishAsync(scheduleMeExchange, conventions.TopicNamingConvention(scheduleMeType), false, false, easyNetQMessage);
            });
        }

        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            return FuturePublishAsync(DateTime.UtcNow.Add(messageDelay), message, cancellationKey);
        }

        public Task CancelFuturePublishAsync(string cancellationKey)
        {
            var uncheduleMeType = typeof(UnscheduleMe);
            return publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, uncheduleMeType, ExchangeType.Topic).Then(unscheduleMeExchange =>
            {
                var unscheduleMe = new UnscheduleMe {CancellationKey = cancellationKey};
                var easyNetQMessage = new Message<UnscheduleMe>(unscheduleMe)
                {
                    Properties =
                    {
                        DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(uncheduleMeType)
                    }
                };
                return advancedBus.PublishAsync(unscheduleMeExchange, conventions.TopicNamingConvention(uncheduleMeType), false, false, easyNetQMessage);
            });
        }
    }
}