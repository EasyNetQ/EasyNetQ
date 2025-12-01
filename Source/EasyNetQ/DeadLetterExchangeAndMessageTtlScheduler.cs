using EasyNetQ.Internals;

namespace EasyNetQ;

/// <summary>
///     Scheduler based on DLE and Message TTL
/// </summary>
public class DeadLetterExchangeAndMessageTtlScheduler : IScheduler
{
    private readonly ConnectionConfiguration configuration;
    private readonly IAdvancedBus advancedBus;
    private readonly IConventions conventions;
    private readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
    private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

    /// <summary>
    ///     Creates DeadLetterExchangeAndMessageTtlScheduler
    /// </summary>
    /// <param name="configuration">The configuration</param>
    /// <param name="advancedBus">The advanced bus</param>
    /// <param name="conventions">The conventions</param>
    /// <param name="messageDeliveryModeStrategy">The message delivery mode strategy</param>
    /// <param name="exchangeDeclareStrategy">The exchange declare strategy</param>
    public DeadLetterExchangeAndMessageTtlScheduler(
        ConnectionConfiguration configuration,
        IAdvancedBus advancedBus,
        IConventions conventions,
        IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
        IExchangeDeclareStrategy exchangeDeclareStrategy
    )
    {
        this.configuration = configuration;
        this.advancedBus = advancedBus;
        this.conventions = conventions;
        this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        this.exchangeDeclareStrategy = exchangeDeclareStrategy;
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
            queue: conventions.QueueNamingConvention(typeof(T), delayString),
            arguments: new Dictionary<string, object>()
                .WithMessageTtl(delay)
                .WithDeadLetterExchange(exchange.Name),
            cancellationToken: cts.Token
        ).ConfigureAwait(false);

        await advancedBus.BindAsync(futureExchange, futureQueue, topic, cts.Token).ConfigureAwait(false);

        var properties = new MessageProperties
        {
            Priority = publishConfiguration.Priority ?? 0,
            Headers = publishConfiguration.MessageHeaders,
            DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(typeof(T)),
        };
        var advancedMessage = new Message<T>(message, properties);
        await advancedBus.PublishAsync(
            futureExchange.Name, topic, null, publishConfiguration.PublisherConfirms, advancedMessage, cts.Token
        ).ConfigureAwait(false);
    }
}
