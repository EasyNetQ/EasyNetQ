﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Scheduler
{
    public class DelayedExchangeScheduler : IScheduler
    {
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

        //TODO Cache exchange/queue/bind
        public async Task FuturePublishAsync<T>(T message, TimeSpan delay, string topic = null, CancellationToken cancellationToken = default) where T : class
        {
            Preconditions.CheckNotNull(message, "message");

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
                    Headers = new Dictionary<string, object> {{"x-delay", (int) delay.TotalMilliseconds}}
                }
            };
            await advancedBus.PublishAsync(futureExchange, topic, false, easyNetQMessage).ConfigureAwait(false);
        }
    }
}