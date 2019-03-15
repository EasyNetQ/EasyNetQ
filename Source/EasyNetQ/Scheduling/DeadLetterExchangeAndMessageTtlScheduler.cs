using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class DeadLetterExchangeAndMessageTtlScheduler : IScheduler
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        public DeadLetterExchangeAndMessageTtlScheduler(
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
        
        //TODO Cache exchange/queue/bind
        public async Task FuturePublishAsync<T>(T message, TimeSpan delay, string topic = null, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(message, "message");

            var delayString = delay.ToString(@"dd\_hh\_mm\_ss");
            var exchangeName = conventions.ExchangeNamingConvention(typeof (T));
            var futureExchangeName = exchangeName + "_" + delayString;
            var futureQueueName = conventions.QueueNamingConvention(typeof (T), delayString);
            var futureExchange = await advancedBus.ExchangeDeclareAsync(futureExchangeName, ExchangeType.Topic, cancellationToken: cancellationToken).ConfigureAwait(false);
            var futureQueue = await advancedBus.QueueDeclareAsync(
                futureQueueName,
                perQueueMessageTtl: (int) delay.TotalMilliseconds, 
                deadLetterExchange: exchangeName,
                deadLetterRoutingKey: topic,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            await advancedBus.BindAsync(futureExchange, futureQueue, topic, cancellationToken).ConfigureAwait(false);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof (T))
                }
            };
            await advancedBus.PublishAsync(futureExchange, topic, false, easyNetQMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}