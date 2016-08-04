using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduling
{
    public class DeadLetterExchangeAndMessageTtlScheduler : IScheduler
    {
        private static readonly TimeSpan MaxMessageDelay = TimeSpan.FromMilliseconds(int.MaxValue);

        private readonly IAdvancedBus advancedBus;
        private readonly IConventions conventions;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        public DeadLetterExchangeAndMessageTtlScheduler(
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
            return FuturePublishAsync(futurePublishDate - DateTime.UtcNow, message, "#", cancellationKey);
        }
        public Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string topic, string cancellationKey = null) where T : class
        {
            return FuturePublishInternalAsync(futurePublishDate - DateTime.UtcNow, message, topic, cancellationKey);
        }

        public void FuturePublish<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            FuturePublishInternal(futurePublishDate - DateTime.UtcNow, message, "#", cancellationKey);
        }
        public void FuturePublish<T>(DateTime futurePublishDate, T message, string topic, string cancellationKey = null) where T : class
        {
            FuturePublishInternal(futurePublishDate - DateTime.UtcNow, message, topic, cancellationKey);
        }

        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            return FuturePublishAsync(messageDelay, message, "#", cancellationKey);
        }
        public Task FuturePublishAsync<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null) where T : class
        {
            return FuturePublishInternalAsync(messageDelay, message, topic, cancellationKey);
        }

        public void FuturePublish<T>(TimeSpan messageDelay, T message, string cancellationKey = null) where T : class
        {
            FuturePublish(messageDelay, message, "#", cancellationKey);
        }
        public void FuturePublish<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null) where T : class
        {
            FuturePublishInternal(messageDelay, message, topic, cancellationKey);
        }

        public Task CancelFuturePublishAsync(string cancellationKey)
        {
            throw new NotImplementedException("Cancellation is not supported");
        }

        public void CancelFuturePublish(string cancellationKey)
        {
            throw new NotImplementedException("Cancellation is not supported");
        }

        private async Task FuturePublishInternalAsync<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckLess(messageDelay, MaxMessageDelay, "messageDelay");
            Preconditions.CheckNull(cancellationKey, "cancellationKey");
            var delay = Round(messageDelay);
            var delayString = delay.ToString(@"dd\_hh\_mm\_ss");
            var exchangeName = conventions.ExchangeNamingConvention(typeof (T));
            var futureExchangeName = exchangeName + "_" + delayString;
            var futureQueueName = conventions.QueueNamingConvention(typeof (T), delayString);
            var futureExchange = await advancedBus.ExchangeDeclareAsync(futureExchangeName, ExchangeType.Topic).ConfigureAwait(false);
            var futureQueue = await advancedBus.QueueDeclareAsync(futureQueueName, perQueueMessageTtl: (int) delay.TotalMilliseconds, deadLetterExchange: exchangeName, deadLetterRoutingKey: topic).ConfigureAwait(false);
            await advancedBus.BindAsync(futureExchange, futureQueue, topic).ConfigureAwait(false);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof (T))
                }
            };
            await advancedBus.PublishAsync(futureExchange, topic, false, easyNetQMessage).ConfigureAwait(false);
        }

        private void FuturePublishInternal<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckLess(messageDelay, MaxMessageDelay, "messageDelay");
            Preconditions.CheckNull(cancellationKey, "cancellationKey");
            var delay = Round(messageDelay);
            var delayString = delay.ToString(@"dd\_hh\_mm\_ss");
            var exchangeName = conventions.ExchangeNamingConvention(typeof (T));
            var futureExchangeName = exchangeName + "_" + delayString;
            var futureQueueName = conventions.QueueNamingConvention(typeof (T), delayString);
            var futureExchange = advancedBus.ExchangeDeclare(futureExchangeName, ExchangeType.Topic);
            var futureQueue = advancedBus.QueueDeclare(futureQueueName, perQueueMessageTtl: (int) delay.TotalMilliseconds, deadLetterExchange: exchangeName, deadLetterRoutingKey: topic);
            advancedBus.Bind(futureExchange, futureQueue, topic);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof (T))
                }
            };
            advancedBus.Publish(futureExchange, topic, false, easyNetQMessage);
        }

        private static TimeSpan Round(TimeSpan timeSpan)
        {
            return new TimeSpan(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, 0);
        }
    }
}