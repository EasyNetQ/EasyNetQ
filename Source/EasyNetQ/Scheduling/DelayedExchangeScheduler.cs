using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class DelayedExchangeScheduler : IScheduler
    {
        private static readonly TimeSpan MaxMessageDelay = TimeSpan.FromMilliseconds(int.MaxValue);

        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        public DelayedExchangeScheduler(
            IAdvancedBus advancedBus,
            IConventions conventions,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");

            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            return FuturePublishInternalAsync(futurePublishDate - DateTime.UtcNow, message, cancellationKey);
        }

        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            return FuturePublishInternalAsync(messageDelay, message, cancellationKey);
        }

        public Task CancelFuturePublishAsync(string cancellationKey)
        {
            throw new NotImplementedException("Cancellation is not supported");
        }

        private Task FuturePublishInternalAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckLess(messageDelay, MaxMessageDelay, "messageDelay");
            Preconditions.CheckNull(cancellationKey, "cancellationKey");
            var exchangeName = conventions.ExchangeNamingConvention(typeof (T));
            var futureExchangeName = exchangeName + "_delayed";
            var queueName = conventions.QueueNamingConvention(typeof (T), null);

            return advancedBus.ExchangeDeclareAsync(futureExchangeName, ExchangeType.Direct, delayed: true)
                .Then(futureExchange => advancedBus.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic)
                    .Then(exchange => advancedBus.BindAsync(futureExchange, exchange, "#")
                        .Then(binding => advancedBus.QueueDeclareAsync(queueName)
                            .Then(queue => advancedBus.BindAsync(exchange, queue, "#"))
                            .Then(() =>
                            {
                                var easyNetQMessage = new Message<T>(message)
                                {
                                    Properties =
                                    {
                                        DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof (T)),
                                        Headers = new Dictionary<string, object> {{"x-delay", (int) messageDelay.TotalMilliseconds}}
                                    }
                                };
                                return advancedBus.PublishAsync(futureExchange, "#", false, false, easyNetQMessage);
                            })
                        )
                    )
                );
        }
    }
}