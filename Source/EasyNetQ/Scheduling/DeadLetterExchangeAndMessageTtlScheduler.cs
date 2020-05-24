using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class DeadLetterExchangeAndMessageTtlScheduler : IScheduler
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        public DeadLetterExchangeAndMessageTtlScheduler(
            ConnectionConfiguration configuration,
            IAdvancedBus advancedBus,
            IConventions conventions,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IExchangeDeclareStrategy exchangeDeclareStrategy
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(exchangeDeclareStrategy, "exchangeDeclareStrategy");

            this.configuration = configuration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.exchangeDeclareStrategy = exchangeDeclareStrategy;
        }

        /// <inheritdoc />
        public async Task FuturePublishAsync<T>(
            T message, TimeSpan delay, string topic, CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                conventions.ExchangeNamingConvention(typeof(T)),
                ExchangeType.Topic,
                cts.Token
            ).ConfigureAwait(false);

            var delayString = delay.ToString(@"dd\_hh\_mm\_ss");
            var futureExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                $"{conventions.ExchangeNamingConvention(typeof(T))}_{delayString}",
                ExchangeType.Topic,
                cts.Token
            ).ConfigureAwait(false);

            var futureQueue = await advancedBus.QueueDeclareAsync(
                conventions.QueueNamingConvention(typeof(T), delayString),
                c => c.WithMessageTtl(delay)
                    .WithDeadLetterExchange(exchange)
                    .WithDeadLetterRoutingKey(topic),
                cts.Token
            ).ConfigureAwait(false);

            await advancedBus.BindAsync(futureExchange, futureQueue, topic, cts.Token).ConfigureAwait(false);

            var advancedMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T))
                }
            };
            await advancedBus.PublishAsync(futureExchange, topic, false, advancedMessage, cts.Token)
                .ConfigureAwait(false);
        }
    }
}
