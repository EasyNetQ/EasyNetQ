using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ;

/// <inheritdoc />
public class DefaultPubSub : IPubSub
{
    private readonly IAdvancedBus advancedBus;
    private readonly ConnectionConfiguration configuration;
    private readonly IConventions conventions;
    private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
    private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;

    /// <summary>
    ///     Creates DefaultPubSub
    /// </summary>
    /// <param name="configuration">The configuration</param>
    /// <param name="conventions">The conventions</param>
    /// <param name="exchangeDeclareStrategy">The exchange declare strategy</param>
    /// <param name="messageDeliveryModeStrategy">The message delivery mode strategy</param>
    /// <param name="advancedBus">The advanced bus</param>
    public DefaultPubSub(
        ConnectionConfiguration configuration,
        IConventions conventions,
        IExchangeDeclareStrategy exchangeDeclareStrategy,
        IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
        IAdvancedBus advancedBus
    )
    {
        Preconditions.CheckNotNull(configuration, nameof(configuration));
        Preconditions.CheckNotNull(conventions, nameof(conventions));
        Preconditions.CheckNotNull(exchangeDeclareStrategy, nameof(exchangeDeclareStrategy));
        Preconditions.CheckNotNull(messageDeliveryModeStrategy, nameof(messageDeliveryModeStrategy));
        Preconditions.CheckNotNull(advancedBus, nameof(advancedBus));

        this.configuration = configuration;
        this.conventions = conventions;
        this.exchangeDeclareStrategy = exchangeDeclareStrategy;
        this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        this.advancedBus = advancedBus;
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync<T>(T message, Action<IPublishConfiguration> configure, CancellationToken cancellationToken)
    {
        Preconditions.CheckNotNull(message, nameof(message));
        Preconditions.CheckNotNull(configure, nameof(configure));

        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var publishConfiguration = new PublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
        configure(publishConfiguration);

        var messageType = typeof(T);
        var advancedMessageProperties = new MessageProperties();
        if (publishConfiguration.Priority != null)
            advancedMessageProperties.Priority = publishConfiguration.Priority.Value;
        if (publishConfiguration.Expires != null)
            advancedMessageProperties.Expiration = publishConfiguration.Expires.ToString();
        if (publishConfiguration.Headers?.Count > 0)
            advancedMessageProperties.Headers.UnionWith(publishConfiguration.Headers);
        advancedMessageProperties.DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType);

        var advancedMessage = new Message<T>(message, advancedMessageProperties);
        var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
            messageType, ExchangeType.Topic, cts.Token
        ).ConfigureAwait(false);
        await advancedBus.PublishAsync(
            exchange, publishConfiguration.Topic, configuration.MandatoryPublish, advancedMessage, cts.Token
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual AwaitableDisposable<SubscriptionResult> SubscribeAsync<T>(
        string subscriptionId,
        Func<T, CancellationToken, Task> onMessage,
        Action<ISubscriptionConfiguration> configure,
        CancellationToken cancellationToken
    )
    {
        Preconditions.CheckNotNull(subscriptionId, nameof(subscriptionId));
        Preconditions.CheckNotNull(onMessage, nameof(onMessage));
        Preconditions.CheckNotNull(configure, nameof(configure));

        return SubscribeAsyncInternal(subscriptionId, onMessage, configure, cancellationToken).ToAwaitableDisposable();
    }

    private async Task<SubscriptionResult> SubscribeAsyncInternal<T>(
        string subscriptionId,
        Func<T, CancellationToken, Task> onMessage,
        Action<ISubscriptionConfiguration> configure,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var subscriptionConfiguration = new SubscriptionConfiguration(configuration.PrefetchCount);
        configure(subscriptionConfiguration);

        var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
        var exchange = await advancedBus.ExchangeDeclareAsync(
            exchangeName,
            c =>
            {
                c.WithType(subscriptionConfiguration.ExchangeType);
                if (!string.IsNullOrEmpty(subscriptionConfiguration.AlternateExchange))
                    c.WithAlternateExchange(new Exchange(subscriptionConfiguration.AlternateExchange));
            },
            cts.Token
        ).ConfigureAwait(false);

        var queueName = subscriptionConfiguration.QueueName ?? conventions.QueueNamingConvention(typeof(T), subscriptionId);
        var queueType = conventions.QueueTypeConvention(typeof(T));
        if (!string.IsNullOrEmpty(queueType))
            subscriptionConfiguration.WithQueueType(queueType);
        var queue = await advancedBus.QueueDeclareAsync(
            queueName,
            c =>
            {
                c.AsExclusive(subscriptionConfiguration.IsExclusive);
                c.AsDurable(subscriptionConfiguration.Durable);
                c.AsAutoDelete(subscriptionConfiguration.AutoDelete);
                if (subscriptionConfiguration.Expires.HasValue)
                    c.WithExpires(TimeSpan.FromMilliseconds(subscriptionConfiguration.Expires.Value));
                if (subscriptionConfiguration.MaxPriority.HasValue)
                    c.WithMaxPriority(subscriptionConfiguration.MaxPriority.Value);
                if (subscriptionConfiguration.MaxLength.HasValue)
                    c.WithMaxLength(subscriptionConfiguration.MaxLength.Value);
                if (subscriptionConfiguration.MaxLengthBytes.HasValue)
                    c.WithMaxLengthBytes(subscriptionConfiguration.MaxLengthBytes.Value);
                if (!string.IsNullOrEmpty(subscriptionConfiguration.QueueMode))
                    c.WithQueueMode(subscriptionConfiguration.QueueMode);
                if (!string.IsNullOrEmpty(subscriptionConfiguration.QueueType))
                    c.WithQueueType(subscriptionConfiguration.QueueType);
            },
            cts.Token
        ).ConfigureAwait(false);

        foreach (var topic in subscriptionConfiguration.Topics.DefaultIfEmpty("#"))
            await advancedBus.BindAsync(exchange, queue, topic, cts.Token).ConfigureAwait(false);

        var consumerCancellation = advancedBus.Consume<T>(
            queue,
            (message, _, cancellation) => onMessage(message.Body, cancellation),
            c => c.WithPrefetchCount(subscriptionConfiguration.PrefetchCount)
                .WithPriority(subscriptionConfiguration.Priority)
                .WithExclusive(subscriptionConfiguration.IsExclusive)
                .WithConsumerTag(conventions.ConsumerTagConvention())
        );

        return new SubscriptionResult(exchange, queue, consumerCancellation);
    }
}
