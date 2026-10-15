namespace EasyNetQ.Configuration;

/// <summary>
///     RabbitMQ-typed publish builder: the exchange settings are strongly typed instead of raw argument
///     dictionaries
/// </summary>
public sealed class RabbitMqPublishBuilder : PublishBuilder<RabbitMqPublishBuilder>
{
    /// <summary>
    ///     Creates the builder
    /// </summary>
    public RabbitMqPublishBuilder(PublishDefinition definition) : base(definition)
    {
    }

    /// <summary>
    ///     Publish to <paramref name="name" />, declared with typed exchange settings
    /// </summary>
    public RabbitMqPublishBuilder Exchange(string name, Action<RabbitMqExchangeBuilder> configure)
    {
        var exchangeBuilder = new RabbitMqExchangeBuilder();
        configure(exchangeBuilder);
        Definition.Exchange = name;
        Definition.ExchangeToDeclare = exchangeBuilder.Build(name);
        return this;
    }
}
