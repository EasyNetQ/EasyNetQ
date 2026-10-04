using EasyNetQ.Transport;

namespace EasyNetQ.Configuration;

/// <summary>
///     RabbitMQ-typed consumer builder: the queue, exchange and consumer settings are strongly typed instead of
///     raw argument dictionaries
/// </summary>
public sealed class RabbitMqConsumerBuilder : ConsumerBuilder<RabbitMqConsumerBuilder>
{
    /// <summary>
    ///     Creates the builder
    /// </summary>
    public RabbitMqConsumerBuilder(ConsumerDefinition definition) : base(definition)
    {
    }

    /// <summary>
    ///     Consume from <paramref name="name" />, declared with typed queue settings
    /// </summary>
    public RabbitMqConsumerBuilder Queue(string name, Action<RabbitMqQueueBuilder> configure)
    {
        var queueBuilder = new RabbitMqQueueBuilder();
        configure(queueBuilder);
        Definition.Queue = name;
        Definition.QueueToDeclare = queueBuilder.Build(name);
        return this;
    }

    /// <summary>
    ///     Bind the queue to <paramref name="exchange" />, declared with typed exchange settings
    /// </summary>
    public RabbitMqConsumerBuilder Bind(string exchange, string routingKey, Action<RabbitMqExchangeBuilder> configure)
    {
        var exchangeBuilder = new RabbitMqExchangeBuilder();
        configure(exchangeBuilder);
        Definition.ExchangesToDeclare.Add(exchangeBuilder.Build(exchange));
        Definition.Bindings.Add(new ConsumerBinding(exchange, routingKey));
        return this;
    }

    /// <summary>
    ///     Consumer tag shown in the management UI
    /// </summary>
    public RabbitMqConsumerBuilder ConsumerTag(string consumerTag)
    {
        Definition.ConfigureContext += context => context.Set(RabbitKeys.ConsumerTag, consumerTag);
        return this;
    }

    /// <summary>
    ///     Only this consumer may consume from the queue
    /// </summary>
    public RabbitMqConsumerBuilder ExclusiveConsumer()
    {
        Definition.ConfigureContext += context => context.Set(RabbitKeys.ExclusiveConsumer, true);
        return this;
    }

    /// <summary>
    ///     basic.consume argument
    /// </summary>
    public RabbitMqConsumerBuilder ConsumerArgument(string name, object value)
    {
        Definition.ConfigureContext += context =>
        {
            if (!context.TryGet(RabbitKeys.ConsumerArguments, out var arguments))
            {
                arguments = new Dictionary<string, object>();
                context.Set(RabbitKeys.ConsumerArguments, arguments);
            }
            arguments[name] = value;
        };
        return this;
    }
}
