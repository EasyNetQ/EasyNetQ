using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Scheduler based on DLE and Message TTL
    /// </summary>
    public class DeadLetterExchangeAndMessageTtlScheduler : IScheduler
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        private readonly bool setDeadLetterRoutingKey;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        /// <summary>
        ///     Creates DeadLetterExchangeAndMessageTtlScheduler
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="advancedBus">The advanced bus</param>
        /// <param name="conventions">The conventions</param>
        /// <param name="messageDeliveryModeStrategy">The message delivery mode strategy</param>
        /// <param name="exchangeDeclareStrategy">The exchange declare strategy</param>
        /// <param name="setDeadLetterRoutingKey">Set deadLetterRoutingKey for backward compability</param>
        public DeadLetterExchangeAndMessageTtlScheduler(
            ConnectionConfiguration configuration,
            IAdvancedBus advancedBus,
            IConventions conventions,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IExchangeDeclareStrategy exchangeDeclareStrategy,
            bool setDeadLetterRoutingKey = false
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
            this.setDeadLetterRoutingKey = setDeadLetterRoutingKey;
        }

        /// <inheritdoc />
        public async Task FuturePublishAsync<T>(
            T message,
            TimeSpan delay,
            Action<IFuturePublishConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(configure, "configure");

            using var cts = cancellationToken.WithTimeout(configuration.Timeout);

            var publishConfiguration = new FuturePublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
            configure(publishConfiguration);

            var topic = publishConfiguration.Topic;
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
                c =>
                {
                    c.WithMessageTtl(delay);
                    c.WithDeadLetterExchange(exchange);
                    if (setDeadLetterRoutingKey)
                        c.WithDeadLetterRoutingKey(topic);
                },
                cts.Token
            ).ConfigureAwait(false);

            await advancedBus.BindAsync(futureExchange, futureQueue, topic, cts.Token).ConfigureAwait(false);

            var properties = new MessageProperties();
            if (publishConfiguration.Priority != null)
                properties.Priority = publishConfiguration.Priority.Value;
            properties.DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T));

            var advancedMessage = new Message<T>(message, properties);
            await advancedBus.PublishAsync(
                futureExchange, topic, configuration.MandatoryPublish, advancedMessage, cts.Token
            ).ConfigureAwait(false);
        }
    }
}
