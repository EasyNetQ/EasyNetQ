using System;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class ExternalSchedulerV2 : IScheduler
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;

        public ExternalSchedulerV2(
            IAdvancedBus advancedBus,
            IConventions conventions,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IMessageSerializationStrategy messageSerializationStrategy)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(messageSerializationStrategy, "messageSerializationStrategy");

            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.messageSerializationStrategy = messageSerializationStrategy;
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            var scheduleMeType = typeof(ScheduleMeV2);
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
                var scheduleMe = new ScheduleMeV2
                {
                    WakeTime = futurePublishDate,
                    CancellationKey = cancellationKey,
                    Message = serializedMessage.Body,
                    MessageProperties = serializedMessage.Properties,
                    ExchangeType = ExchangeType.Topic,
                    Exchange = conventions.ExchangeNamingConvention(baseMessageType),
                    RoutingKey = "#"
                };
                var scheduleMeMessage = new Message<ScheduleMeV2>(scheduleMe)
                {
                    Properties =
                    {
                        DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(scheduleMeType)
                    }
                };
                return advancedBus.PublishAsync(scheduleMeExchange, conventions.TopicNamingConvention(scheduleMeType), false, false, scheduleMeMessage);
            });
        }

        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            return FuturePublishAsync(DateTime.UtcNow.Add(messageDelay), message);
        }

        public Task CancelFuturePublishAsync(string cancellationKey)
        {
            var uncheduleMeType = typeof (UnscheduleMe);
            return publishExchangeDeclareStrategy.DeclareExchangeAsync(advancedBus, uncheduleMeType, ExchangeType.Topic).Then(unscheduleMeExchange =>
            {
                var unscheduleMe = new UnscheduleMeV2
                {
                    CancellationKey = cancellationKey
                };
                var unscheduleMeMessage = new Message<UnscheduleMeV2>(unscheduleMe)
                {
                    Properties =
                    {
                        DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(uncheduleMeType)
                    }
                };
                return advancedBus.PublishAsync(unscheduleMeExchange, conventions.TopicNamingConvention(uncheduleMeType), false, false, unscheduleMeMessage);
            });
        }
    }
}