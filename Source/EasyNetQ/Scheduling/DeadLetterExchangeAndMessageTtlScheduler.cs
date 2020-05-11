using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class DeadLetterExchangeAndMessageTtlScheduler : IScheduler
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        public DeadLetterExchangeAndMessageTtlScheduler(
            IAdvancedBus advancedBus,
            IConventions conventions,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IExchangeDeclareStrategy exchangeDeclareStrategy
        )
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(exchangeDeclareStrategy, "exchangeDeclareStrategy");

            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.exchangeDeclareStrategy = exchangeDeclareStrategy;
        }

        public async Task FuturePublishAsync<T>(T message, TimeSpan delay, string topic = "",
            CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                conventions.ExchangeNamingConvention(typeof(T)),
                ExchangeType.Topic,
                cancellationToken
            ).ConfigureAwait(false);

            var delayString = delay.ToString(@"dd\_hh\_mm\_ss");
            var futureExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                $"{conventions.ExchangeNamingConvention(typeof(T))}_{delayString}",
                ExchangeType.Topic,
                cancellationToken
            ).ConfigureAwait(false);

            var futureQueue = await advancedBus.QueueDeclareAsync(
                conventions.QueueNamingConvention(typeof(T), delayString),
                c => c.WithMessageTtl(delay)
                    .WithDeadLetterExchange(exchange)
                    .WithDeadLetterRoutingKey(topic),
                cancellationToken
            ).ConfigureAwait(false);

            await advancedBus.BindAsync(futureExchange, futureQueue, topic, cancellationToken).ConfigureAwait(false);

            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T))
                }
            };
            await advancedBus.PublishAsync(futureExchange, topic, false, easyNetQMessage, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
