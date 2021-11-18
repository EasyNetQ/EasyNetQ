using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Scheduler based on delayed exchange
    /// </summary>
    public class DelayedExchangeScheduler : IScheduler
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        /// <summary>
        ///     Creates DelayedExchangeScheduler
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="advancedBus">The advanced bus</param>
        /// <param name="conventions">The conventions</param>
        /// <param name="messageDeliveryModeStrategy">The message delivery mode strategy</param>
        public DelayedExchangeScheduler(
            ConnectionConfiguration configuration,
            IAdvancedBus advancedBus,
            IConventions conventions,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy
        )
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            Preconditions.CheckNotNull(advancedBus, nameof(advancedBus));
            Preconditions.CheckNotNull(conventions, nameof(conventions));
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, nameof(messageDeliveryModeStrategy));

            this.configuration = configuration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        }

        /// <inheritdoc />
        public async Task FuturePublishAsync<T>(
            T message,
            TimeSpan delay,
            Action<IFuturePublishConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotNull(configure, nameof(configure));

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var publishConfiguration = new FuturePublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
            configure(publishConfiguration);

            var topic = publishConfiguration.Topic;
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var futureExchangeName = exchangeName + "_delayed";
            var futureExchange = await advancedBus.ExchangeDeclareAsync(
                futureExchangeName,
                c => c.AsDelayedExchange(ExchangeType.Topic),
                cts.Token
            ).ConfigureAwait(false);

            var exchange = await advancedBus.ExchangeDeclareAsync(
                exchangeName,
                c => c.WithType(ExchangeType.Topic),
                cts.Token
            ).ConfigureAwait(false);
            await advancedBus.BindAsync(futureExchange, exchange, topic, cts.Token).ConfigureAwait(false);

            var properties = new MessageProperties();
            if (publishConfiguration.Priority != null)
                properties.Priority = publishConfiguration.Priority.Value;
            if (publishConfiguration.Headers?.Count > 0)
                properties.Headers.UnionWith(publishConfiguration.Headers);
            properties.DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T));

            await advancedBus.PublishAsync(
                futureExchange, topic, configuration.MandatoryPublish, new Message<T>(message, properties).WithDelay(delay), cts.Token
            ).ConfigureAwait(false);
        }
    }
}
