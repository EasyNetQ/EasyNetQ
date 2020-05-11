using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class DelayedExchangeScheduler : IScheduler
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        public DelayedExchangeScheduler(
            IAdvancedBus advancedBus,
            IConventions conventions,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy
        )
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");

            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        }

        public async Task FuturePublishAsync<T>(T message, TimeSpan delay, string topic, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var futureExchangeName = exchangeName + "_delayed";
            var futureExchange = await advancedBus.ExchangeDeclareAsync(
                futureExchangeName,
                c => c.AsDelayedExchange(ExchangeType.Direct),
                cancellationToken
            ).ConfigureAwait(false);
            var exchange = await advancedBus.ExchangeDeclareAsync(
                exchangeName,
                c => c.WithType(ExchangeType.Topic),
                cancellationToken
            ).ConfigureAwait(false);
            await advancedBus.BindAsync(futureExchange, exchange, topic, cancellationToken).ConfigureAwait(false);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T)),
                }
            };
            await advancedBus.PublishAsync(futureExchange, topic, false, easyNetQMessage.WithDelay(delay), cancellationToken).ConfigureAwait(false);
        }
    }
}
