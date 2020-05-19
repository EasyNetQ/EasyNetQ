using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class DelayedExchangeScheduler : IScheduler
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        public DelayedExchangeScheduler(
            ConnectionConfiguration configuration,
            IAdvancedBus advancedBus,
            IConventions conventions,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");

            this.configuration = configuration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        }

        /// <inheritdoc />
        public async Task FuturePublishAsync<T>(
            T message, TimeSpan delay, string topic, CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (configuration.Timeout != Timeout.InfiniteTimeSpan)
                cts.CancelAfter(configuration.Timeout);

            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var futureExchangeName = exchangeName + "_delayed";
            var futureExchange = await advancedBus.ExchangeDeclareAsync(
                futureExchangeName,
                c => c.AsDelayedExchange(ExchangeType.Direct),
                cts.Token
            ).ConfigureAwait(false);
            var exchange = await advancedBus.ExchangeDeclareAsync(
                exchangeName,
                c => c.WithType(ExchangeType.Topic),
                cts.Token
            ).ConfigureAwait(false);
            await advancedBus.BindAsync(futureExchange, exchange, topic, cts.Token).ConfigureAwait(false);
            var advancedMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T)),
                }
            };
            await advancedBus.PublishAsync(futureExchange, topic, false, advancedMessage.WithDelay(delay), cts.Token).ConfigureAwait(false);
        }
    }
}
