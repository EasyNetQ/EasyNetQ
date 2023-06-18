using EasyNetQ.Internals;

namespace EasyNetQ;

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
        using var cts = cancellationToken.WithTimeout(configuration.Timeout);

        var publishConfiguration = new FuturePublishConfiguration(conventions.TopicNamingConvention(typeof(T)));
        configure(publishConfiguration);

        var topic = publishConfiguration.Topic;
        var exchangeName = conventions.ExchangeNamingConvention(typeof(T));
        var futureExchangeName = exchangeName + "_delayed";
        var futureExchange = await advancedBus.ExchangeDeclareAsync(
            exchange: futureExchangeName,
            type: ExchangeType.DelayedMessage,
            arguments: new Dictionary<string, object>().WithDelayedType(ExchangeType.Topic),
            cancellationToken: cts.Token
        ).ConfigureAwait(false);

        var exchange = await advancedBus.ExchangeDeclareAsync(
            exchange: exchangeName,
            cancellationToken: cts.Token
        ).ConfigureAwait(false);
        await advancedBus.BindAsync(futureExchange, exchange, topic, cts.Token).ConfigureAwait(false);

        var properties = new MessageProperties
        {
            Priority = publishConfiguration.Priority ?? 0,
            Headers = publishConfiguration.MessageHeaders,
            DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T))
        }.WithDelay(delay);

        await advancedBus.PublishAsync(
            futureExchange.Name, topic, null, publishConfiguration.PublisherConfirms, new Message<T>(message, properties), cts.Token
        ).ConfigureAwait(false);
    }
}
