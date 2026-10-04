using EasyNetQ.Transport;

namespace EasyNetQ.Configuration;

/// <summary>
///     Typed configuration of a RabbitMQ queue declaration; writes into the queue definition's arguments
/// </summary>
public sealed class RabbitMqQueueBuilder
{
    private bool durable = true;
    private bool exclusive;
    private bool autoDelete;
    private readonly Dictionary<string, object> arguments = new();

    /// <summary>Survives a broker restart (default true)</summary>
    public RabbitMqQueueBuilder Durable(bool isDurable = true)
    {
        durable = isDurable;
        return this;
    }

    /// <summary>Only this connection may use the queue; deleted when it closes</summary>
    public RabbitMqQueueBuilder Exclusive(bool isExclusive = true)
    {
        exclusive = isExclusive;
        return this;
    }

    /// <summary>Deleted when the last consumer disconnects</summary>
    public RabbitMqQueueBuilder AutoDelete(bool isAutoDelete = true)
    {
        autoDelete = isAutoDelete;
        return this;
    }

    /// <summary>Quorum queue</summary>
    public RabbitMqQueueBuilder Quorum() => Argument(EasyNetQ.Argument.QueueType, QueueType.Quorum);

    /// <summary>Classic queue</summary>
    public RabbitMqQueueBuilder Classic() => Argument(EasyNetQ.Argument.QueueType, QueueType.Classic);

    /// <summary>Stream</summary>
    public RabbitMqQueueBuilder Stream() => Argument(EasyNetQ.Argument.QueueType, QueueType.Stream);

    /// <summary>Dead letter exchange for rejected/expired messages</summary>
    public RabbitMqQueueBuilder DeadLetterExchange(string exchange) => Argument(EasyNetQ.Argument.DeadLetterExchange, exchange);

    /// <summary>Routing key for dead-lettered messages</summary>
    public RabbitMqQueueBuilder DeadLetterRoutingKey(string routingKey) => Argument(EasyNetQ.Argument.DeadLetterRoutingKey, routingKey);

    /// <summary>Per-queue message TTL</summary>
    public RabbitMqQueueBuilder MessageTtl(TimeSpan ttl) => Argument(EasyNetQ.Argument.MessageTtl, (int)ttl.TotalMilliseconds);

    /// <summary>Queue expiry when unused</summary>
    public RabbitMqQueueBuilder Expires(TimeSpan expires) => Argument(EasyNetQ.Argument.Expires, (int)expires.TotalMilliseconds);

    /// <summary>Maximum priority the queue supports</summary>
    public RabbitMqQueueBuilder MaxPriority(byte maxPriority) => Argument(EasyNetQ.Argument.MaxPriority, (int)maxPriority);

    /// <summary>Maximum number of ready messages</summary>
    public RabbitMqQueueBuilder MaxLength(int maxLength) => Argument(EasyNetQ.Argument.MaxLength, maxLength);

    /// <summary>Maximum total body size of ready messages</summary>
    public RabbitMqQueueBuilder MaxLengthBytes(int maxLengthBytes) => Argument(EasyNetQ.Argument.MaxLengthBytes, maxLengthBytes);

    /// <summary>Only one consumer at a time receives messages</summary>
    public RabbitMqQueueBuilder SingleActiveConsumer() => Argument(EasyNetQ.Argument.SingleActiveConsumer, true);

    /// <summary>Any x-argument</summary>
    public RabbitMqQueueBuilder Argument(string name, object value)
    {
        arguments[name] = value;
        return this;
    }

    internal QueueDefinition Build(string name) => new(name, durable, exclusive, autoDelete)
    {
        Arguments = arguments.Count > 0 ? arguments : null
    };
}

/// <summary>
///     Typed configuration of a RabbitMQ exchange declaration
/// </summary>
public sealed class RabbitMqExchangeBuilder
{
    private string type = ExchangeType.Topic;
    private bool durable = true;
    private bool autoDelete;
    private Dictionary<string, object>? arguments;

    /// <summary>Topic exchange (default)</summary>
    public RabbitMqExchangeBuilder Topic()
    {
        type = ExchangeType.Topic;
        return this;
    }

    /// <summary>Direct exchange</summary>
    public RabbitMqExchangeBuilder Direct()
    {
        type = ExchangeType.Direct;
        return this;
    }

    /// <summary>Fanout exchange</summary>
    public RabbitMqExchangeBuilder Fanout()
    {
        type = ExchangeType.Fanout;
        return this;
    }

    /// <summary>Survives a broker restart (default true)</summary>
    public RabbitMqExchangeBuilder Durable(bool isDurable = true)
    {
        durable = isDurable;
        return this;
    }

    /// <summary>Deleted when the last binding is removed</summary>
    public RabbitMqExchangeBuilder AutoDelete(bool isAutoDelete = true)
    {
        autoDelete = isAutoDelete;
        return this;
    }

    /// <summary>Unroutable messages go to this exchange</summary>
    public RabbitMqExchangeBuilder AlternateExchange(string exchange) => Argument(EasyNetQ.Argument.AlternateExchange, exchange);

    /// <summary>Any exchange argument</summary>
    public RabbitMqExchangeBuilder Argument(string name, object value)
    {
        (arguments ??= new Dictionary<string, object>())[name] = value;
        return this;
    }

    internal ExchangeDefinition Build(string name) => new(name, type, durable, autoDelete) { Arguments = arguments };
}
