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
            return FuturePublishAsync(futurePublishDate, message, "#", cancellationKey);
        }

        public Task FuturePublishAsync<T>(DateTime futurePublishDate, T message, string topic, string cancellationKey = null) where T : class
        {
            return FuturePublishInternalAsync(futurePublishDate - DateTime.UtcNow, message, topic, cancellationKey);
        }
        public void FuturePublish<T>(DateTime futurePublishDate, T message, string cancellationKey = null) where T : class
        {
            FuturePublish(futurePublishDate, message, "#", cancellationKey);
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

        private async Task FuturePublishInternalAsync<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckLess(messageDelay, MaxMessageDelay, "messageDelay");
            Preconditions.CheckNull(cancellationKey, "cancellationKey");

            var exchangeName = conventions.ExchangeNamingConvention(typeof (T));
            var futureExchangeName = exchangeName + "_delayed";
            var queueName = conventions.QueueNamingConvention(typeof (T), null);
            var futureExchange = await advancedBus.ExchangeDeclareAsync(futureExchangeName, ExchangeType.Direct, delayed: true).ConfigureAwait(false);
            var exchange = await advancedBus.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic).ConfigureAwait(false);
            await advancedBus.BindAsync(futureExchange, exchange, topic).ConfigureAwait(false);
            var queue = await advancedBus.QueueDeclareAsync(queueName).ConfigureAwait(false);
            await advancedBus.BindAsync(exchange, queue, topic).ConfigureAwait(false);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof (T)),
                    Headers = new Dictionary<string, object> {{"x-delay", (int) messageDelay.TotalMilliseconds}}
                }
            };
            await advancedBus.PublishAsync(futureExchange, topic, false, easyNetQMessage).ConfigureAwait(false);
        }

        private void FuturePublishInternal<T>(TimeSpan messageDelay, T message, string topic, string cancellationKey = null) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckLess(messageDelay, MaxMessageDelay, "messageDelay");
            Preconditions.CheckNull(cancellationKey, "cancellationKey");

            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
            var futureExchangeName = exchangeName + "_delayed";
            var queueName = conventions.QueueNamingConvention(typeof(T), null);
            var futureExchange = advancedBus.ExchangeDeclare(futureExchangeName, ExchangeType.Direct, delayed: true);
            var exchange = advancedBus.ExchangeDeclare(exchangeName, ExchangeType.Topic);
            advancedBus.Bind(futureExchange, exchange, topic);
            var queue = advancedBus.QueueDeclare(queueName);
            advancedBus.Bind(exchange, queue, topic);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T)),
                    Headers = new Dictionary<string, object> { { "x-delay", (int)messageDelay.TotalMilliseconds } }
                }
            };
            advancedBus.Publish(futureExchange, topic, false, easyNetQMessage);
        }

    }
}