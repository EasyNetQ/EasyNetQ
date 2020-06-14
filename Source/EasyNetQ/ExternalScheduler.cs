using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Scheduler based on external scheduler service
    /// </summary>
    public class ExternalScheduler : IScheduler
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        private readonly IMessageSerializationStrategy messageSerializationStrategy;

        /// <summary>
        ///     Creates ExternalScheduler
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="advancedBus">The advanced bus</param>
        /// <param name="conventions">The conventions</param>
        /// <param name="exchangeDeclareStrategy">The exchange declare strategy</param>
        /// <param name="messageDeliveryModeStrategy">The message delivery mode strategy</param>
        /// <param name="messageSerializationStrategy">The message serialization strategy</param>
        public ExternalScheduler(
            ConnectionConfiguration configuration,
            IAdvancedBus advancedBus,
            IConventions conventions,
            IExchangeDeclareStrategy exchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IMessageSerializationStrategy messageSerializationStrategy
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            Preconditions.CheckNotNull(advancedBus, nameof(advancedBus));
            Preconditions.CheckNotNull(conventions, nameof(conventions));
            Preconditions.CheckNotNull(exchangeDeclareStrategy, nameof(exchangeDeclareStrategy));
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, nameof(messageDeliveryModeStrategy));
            Preconditions.CheckNotNull(messageSerializationStrategy, nameof(messageSerializationStrategy));

            this.configuration = configuration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.exchangeDeclareStrategy = exchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.messageSerializationStrategy = messageSerializationStrategy;
        }

        /// <inheritdoc />
        public async Task FuturePublishAsync<T>(T message, TimeSpan delay, string topic, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var scheduleMeType = typeof(ScheduleMe);
            var scheduleMeExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                scheduleMeType, ExchangeType.Topic, cts.Token
            ).ConfigureAwait(false);
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
                ExchangeType = ExchangeType.Topic,
                Exchange = conventions.ExchangeNamingConvention(baseMessageType),
                RoutingKey = topic
            };
            var advancedMessage = new Message<ScheduleMe>(scheduleMe)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(scheduleMeType)
                }
            };
            await advancedBus.PublishAsync(
                scheduleMeExchange, conventions.TopicNamingConvention(scheduleMeType), false, advancedMessage, cts.Token
            ).ConfigureAwait(false);
        }
    }
}
