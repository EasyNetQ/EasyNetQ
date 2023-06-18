using EasyNetQ.Internals;

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
        this.configuration = configuration;
        this.conventions = conventions;
        this.exchangeDeclareStrategy = exchangeDeclareStrategy;
        this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        this.advancedBus = advancedBus;
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync<T>(T message, Action<IPublishConfiguration> configure, CancellationToken cancellationToken)
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var publishConfiguration = new PublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
        configure(publishConfiguration);

        var messageType = typeof(T);
        var advancedMessageProperties = new MessageProperties
        {
            Priority = publishConfiguration.Priority ?? 0,
            Expiration = publishConfiguration.Expires,
            Headers = publishConfiguration.MessageHeaders,
            DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType),
        };
        var advancedMessage = new Message<T>(message, advancedMessageProperties);
        var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
            messageType, ExchangeType.Topic, cts.Token
        ).ConfigureAwait(false);
        await advancedBus.PublishAsync(
            exchange.Name, publishConfiguration.Topic, null, publishConfiguration.PublisherConfirms, advancedMessage, cts.Token
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual Task<SubscriptionResult> SubscribeAsync<T>(
        string subscriptionId,
        Func<T, CancellationToken, Task> onMessage,
        Action<ISubscriptionConfiguration> configure,
        CancellationToken cancellationToken
    ) => SubscribeAsyncInternal(subscriptionId, onMessage, configure, cancellationToken);

    private async Task<SubscriptionResult> SubscribeAsyncInternal<T>(
        string subscriptionId,
        Func<T, CancellationToken, Task> onMessage,
        Action<ISubscriptionConfiguration> configure,
        CancellationToken cancellationToken
    )
    {
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var subscriptionConfiguration = new SubscriptionConfiguration(configuration.PrefetchCount, conventions.QueueTypeConvention(typeof(T)));
        configure(subscriptionConfiguration);

        var exchange = await advancedBus.ExchangeDeclareAsync(
            exchange: conventions.ExchangeNamingConvention(typeof(T)),
            type: subscriptionConfiguration.ExchangeType,
            arguments: subscriptionConfiguration.ExchangeArguments,
            cancellationToken: cts.Token
        ).ConfigureAwait(false);

        var queue = await advancedBus.QueueDeclareAsync(
            queue: subscriptionConfiguration.QueueName ?? conventions.QueueNamingConvention(typeof(T), subscriptionId),
            durable: subscriptionConfiguration.Durable,
            autoDelete: subscriptionConfiguration.AutoDelete,
            arguments: subscriptionConfiguration.QueueArguments,
            cancellationToken: cts.Token
        ).ConfigureAwait(false);

        foreach (var topic in subscriptionConfiguration.Topics.DefaultIfEmpty("#"))
            await advancedBus.BindAsync(exchange, queue, topic, cts.Token).ConfigureAwait(false);

        var consumerCancellation = advancedBus.Consume<T>(
            queue,
            (message, _, cancellation) => onMessage(message.Body!, cancellation),
            c => c.WithPrefetchCount(subscriptionConfiguration.PrefetchCount)
                .WithPriority(subscriptionConfiguration.Priority)
                .WithExclusive(subscriptionConfiguration.IsExclusive)
                .WithConsumerTag(conventions.ConsumerTagConvention())
        );

        return new SubscriptionResult(exchange, queue, consumerCancellation);
    }
}
