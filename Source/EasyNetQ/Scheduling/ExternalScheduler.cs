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
        private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;

        public ExternalScheduler(
            IAdvancedBus advancedBus,
            IConventions conventions,
            ITypeNameSerializer typeNameSerializer,
            IExchangeDeclareStrategy exchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IMessageSerializationStrategy messageSerializationStrategy)
        {
            Preconditions.CheckNotNull(advancedBus, nameof(advancedBus));
            Preconditions.CheckNotNull(conventions, nameof(conventions));
            Preconditions.CheckNotNull(exchangeDeclareStrategy, nameof(exchangeDeclareStrategy));
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, nameof(messageDeliveryModeStrategy));
            Preconditions.CheckNotNull(messageSerializationStrategy, nameof(messageSerializationStrategy));
            Preconditions.CheckNotNull(typeNameSerializer, nameof(typeNameSerializer));

            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.typeNameSerializer = typeNameSerializer;
            this.exchangeDeclareStrategy = exchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.messageSerializationStrategy = messageSerializationStrategy;
        }

        public async Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            await FuturePublishAsync(futurePublishDate, message, "#", cancellationKey);
        }
        public async Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string topic, string cancellationKey = null) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            var scheduleMeType = typeof(ScheduleMe);
            var scheduleMeExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(scheduleMeType, ExchangeType.Topic).ConfigureAwait(false);
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
                BindingKey = typeNameSerializer.Serialize(typeof(T)),
                ExchangeType = ExchangeType.Topic,
                Exchange = conventions.ExchangeNamingConvention(baseMessageType),
                RoutingKey = topic
            };
            var easyNetQMessage = new Message<ScheduleMe>(scheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(scheduleMeType)
                }
            };
            await advancedBus.PublishAsync(scheduleMeExchange, conventions.TopicNamingConvention(scheduleMeType), false, easyNetQMessage).ConfigureAwait(false);
        }


        public void FuturePublish<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            FuturePublish(futurePublishDate, message, "#", cancellationKey);
        }
        public void FuturePublish<T>(DateTime futurePublishDate, T message, string topic, string cancellationKey = null) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            var scheduleMeType = typeof(ScheduleMe);
            var scheduleMeExchange = exchangeDeclareStrategy.DeclareExchange(scheduleMeType, ExchangeType.Topic);
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
                BindingKey = typeNameSerializer.Serialize(typeof(T)),
                ExchangeType = ExchangeType.Topic,
                Exchange = conventions.ExchangeNamingConvention(baseMessageType),
                RoutingKey = topic
            };
            var easyNetQMessage = new Message<ScheduleMe>(scheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(scheduleMeType)
                }
            };
            advancedBus.Publish(scheduleMeExchange, conventions.TopicNamingConvention(scheduleMeType), false, easyNetQMessage);
        }


        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            return FuturePublishAsync(messageDelay, message, "#", cancellationKey);
        }
        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null) where T : class
        {
            return FuturePublishAsync(DateTime.UtcNow.Add(messageDelay), message, topic, cancellationKey);
        }

        public void FuturePublish<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            FuturePublish(messageDelay, message, "#", cancellationKey);
        }
        public void FuturePublish<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null) where T : class
        {
            FuturePublish(DateTime.UtcNow.Add(messageDelay), message, topic, cancellationKey);
        }

        public async Task CancelFuturePublishAsync(string cancellationKey)
        {
            var uncheduleMeType = typeof(UnscheduleMe);
            var unscheduleMeExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(uncheduleMeType, ExchangeType.Topic).ConfigureAwait(false);
            var unscheduleMe = new UnscheduleMe { CancellationKey = cancellationKey };
            var easyNetQMessage = new Message<UnscheduleMe>(unscheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(uncheduleMeType)
                }
            };
            await advancedBus.PublishAsync(unscheduleMeExchange, conventions.TopicNamingConvention(uncheduleMeType), false, easyNetQMessage).ConfigureAwait(false);
        }

        public void CancelFuturePublish(string cancellationKey)
        {
            var uncheduleMeType = typeof(UnscheduleMe);
            var unscheduleMeExchange = exchangeDeclareStrategy.DeclareExchange(uncheduleMeType, ExchangeType.Topic);
            var unscheduleMe = new UnscheduleMe { CancellationKey = cancellationKey };
            var easyNetQMessage = new Message<UnscheduleMe>(unscheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(uncheduleMeType)
                }
            };
            advancedBus.Publish(unscheduleMeExchange, conventions.TopicNamingConvention(uncheduleMeType), false, easyNetQMessage);
        }
    }
}
