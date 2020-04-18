using System;
using System.Threading;
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
        private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;
        private readonly ITypeNameSerializer typeNameSerializer;

        public ExternalScheduler(
            IAdvancedBus advancedBus,
            IConventions conventions,
            ITypeNameSerializer typeNameSerializer,
            IExchangeDeclareStrategy exchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IMessageSerializationStrategy messageSerializationStrategy
        )
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

        //TODO Cache exchange
        public async Task FuturePublishAsync<T>(T message, TimeSpan delay, string topic = null, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, "message");

            var scheduleMeType = typeof(ScheduleMe);
            var scheduleMeExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(scheduleMeType, ExchangeType.Topic, cancellationToken).ConfigureAwait(false);
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
                WakeTime = DateTime.UtcNow.Add(delay),
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
            await advancedBus.PublishAsync(scheduleMeExchange, conventions.TopicNamingConvention(scheduleMeType), false, easyNetQMessage, cancellationToken).ConfigureAwait(false);
        }

        public async Task CancelFuturePublishAsync(string cancellationKey, CancellationToken cancellationToken = default)
        {
            var uncheduleMeType = typeof(UnscheduleMe);
            var unscheduleMeExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(uncheduleMeType, ExchangeType.Topic, cancellationToken).ConfigureAwait(false);
            var unscheduleMe = new UnscheduleMe { CancellationKey = cancellationKey };
            var easyNetQMessage = new Message<UnscheduleMe>(unscheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(uncheduleMeType)
                }
            };
            await advancedBus.PublishAsync(unscheduleMeExchange, conventions.TopicNamingConvention(uncheduleMeType), false, easyNetQMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}
