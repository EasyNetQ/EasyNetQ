﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.PubSub
{
    public class DefaultPubSub : IPubSub
    {
        private readonly ConnectionConfiguration connectionConfiguration;
        private readonly IConventions conventions;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly IAdvancedBus advancedBus;

        public DefaultPubSub(
            ConnectionConfiguration connectionConfiguration,
            IConventions conventions,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            IAdvancedBus advancedBus
        )
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            
            this.connectionConfiguration = connectionConfiguration;
            this.conventions = conventions;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.advancedBus = advancedBus;
        }

        public virtual async Task PublishAsync<T>(T message, Action<IPublishConfiguration> configure, CancellationToken cancellationToken) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(configure, "configure");

            var configuration = new PublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
            configure(configuration);

            var messageType = typeof(T);
            var easyNetQMessage = new Message<T>(message)
            {
                Properties =
                {
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType)
                }
            };
            if (configuration.Priority != null)
                easyNetQMessage.Properties.Priority = configuration.Priority.Value;
            if (configuration.Expires != null)
                easyNetQMessage.Properties.Expiration = configuration.Expires.ToString();

            var exchange = await publishExchangeDeclareStrategy.DeclareExchangeAsync(messageType, ExchangeType.Topic, cancellationToken).ConfigureAwait(false);
            await advancedBus.PublishAsync(exchange, configuration.Topic, false, easyNetQMessage, cancellationToken).ConfigureAwait(false);
        }

        public virtual AwaitableDisposable<ISubscriptionResult> SubscribeAsync<T>(
            string subscriptionId,
            Func<T, CancellationToken, Task> onMessage,
            Action<ISubscriptionConfiguration> configure,
            CancellationToken cancellationToken
        ) where T : class
        {
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            return SubscribeAsyncInternal(subscriptionId, onMessage, configure, cancellationToken).ToAwaitableDisposable();
        }

        private async Task<ISubscriptionResult> SubscribeAsyncInternal<T>(
            string subscriptionId,
            Func<T, CancellationToken, Task> onMessage, 
            Action<ISubscriptionConfiguration> configure,
            CancellationToken cancellationToken
        ) where T : class
        {
            var configuration = new SubscriptionConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration);

            var queueName = configuration.QueueName ?? conventions.QueueNamingConvention(typeof(T), subscriptionId);
            var exchangeName = conventions.ExchangeNamingConvention(typeof(T));

            var queue = await advancedBus.QueueDeclareAsync(
                queueName, 
                autoDelete: configuration.AutoDelete, 
                durable: configuration.Durable, 
                expires: configuration.Expires, 
                maxPriority: configuration.MaxPriority,
                maxLength: configuration.MaxLength,
                maxLengthBytes: configuration.MaxLengthBytes,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            
            var exchange = await advancedBus.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var topic in configuration.Topics.DefaultIfEmpty("#"))
            {
                await advancedBus.BindAsync(exchange, queue, topic, cancellationToken).ConfigureAwait(false);
            }

            var consumerCancellation = advancedBus.Consume<T>(
                queue,
                (message, messageReceivedInfo) => onMessage(message.Body, default),
                x => x.WithPriority(configuration.Priority)
                      .WithPrefetchCount(configuration.PrefetchCount)
                      .WithExclusive(configuration.IsExclusive)
            );
            
            return new SubscriptionResult(exchange, queue, consumerCancellation);
        }
    }
}