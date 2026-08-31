using EasyNetQ.Pipeline;
using EasyNetQ.Transport;

namespace EasyNetQ.Configuration;

/// <summary>
///     Base of the fluent consumer builders. Transports derive typed builders
///     (<c>UseRabbitMq(r =&gt; r.Consume(c =&gt; c.Queue("q", q =&gt; q.Quorum())))</c>); the shared members return
///     <typeparamref name="TSelf" /> so a chain never degrades to the base type.
/// </summary>
public abstract class ConsumerBuilder<TSelf> where TSelf : ConsumerBuilder<TSelf>
{
    /// <summary>
    ///     Creates a builder over <paramref name="definition" />
    /// </summary>
    protected ConsumerBuilder(ConsumerDefinition definition) => Definition = definition;

    /// <summary>
    ///     The definition being built
    /// </summary>
    public ConsumerDefinition Definition { get; }

    private TSelf Self => (TSelf)this;

    /// <summary>
    ///     Consume from <paramref name="name" />, declaring it at startup
    /// </summary>
    public TSelf Queue(string name)
    {
        Definition.Queue = name;
        Definition.QueueToDeclare = new QueueDefinition(name);
        return Self;
    }

    /// <summary>
    ///     Consume from <paramref name="name" /> without declaring it
    /// </summary>
    public TSelf ExistingQueue(string name)
    {
        Definition.Queue = name;
        Definition.QueueToDeclare = null;
        return Self;
    }

    /// <summary>
    ///     Bind the queue to <paramref name="exchange" /> with <paramref name="routingKey" />; the exchange is
    ///     declared at startup
    /// </summary>
    public TSelf Bind(string exchange, string routingKey)
    {
        Definition.ExchangesToDeclare.Add(new ExchangeDefinition(exchange));
        Definition.Bindings.Add(new ConsumerBinding(exchange, routingKey));
        return Self;
    }

    /// <summary>
    ///     Prefetch for this consumer
    /// </summary>
    public TSelf PrefetchCount(ushort prefetchCount)
    {
        Definition.PrefetchCount = prefetchCount;
        return Self;
    }

    /// <summary>
    ///     Acknowledge automatically on delivery
    /// </summary>
    public TSelf AutoAck()
    {
        Definition.AutoAck = true;
        return Self;
    }

    /// <summary>
    ///     Handle messages of <typeparamref name="T" />; the handler decides the acknowledgement
    /// </summary>
    public TSelf Handle<T>(MessageHandler<T> handler)
    {
        Definition.HandlerRegistrations.Add((_, table) => table.Add(handler));
        return Self;
    }

    /// <summary>
    ///     Handle messages of <typeparamref name="T" />; completion acknowledges, an exception goes to the
    ///     error strategy
    /// </summary>
    public TSelf Handle<T>(Func<T, CancellationToken, Task> handler)
    {
        Definition.HandlerRegistrations.Add((_, table) => table.Add<T>(async (message, context) =>
        {
            await handler(message, context.CancellationToken).ConfigureAwait(false);
            return AckDecision.Ack;
        }));
        return Self;
    }

    /// <summary>
    ///     Customize the message pipeline (runs after the typed dispatch steps are registered, so
    ///     InsertBefore/InsertAfter can target them)
    /// </summary>
    public TSelf Message(Action<PipelineBuilder<ConsumeContext>> configure)
    {
        Definition.MessagePipeline += configure;
        return Self;
    }
}

/// <summary>
///     The transport-agnostic consumer builder
/// </summary>
public sealed class GenericConsumerBuilder : ConsumerBuilder<GenericConsumerBuilder>
{
    /// <summary>
    ///     Creates the builder
    /// </summary>
    public GenericConsumerBuilder(ConsumerDefinition definition) : base(definition)
    {
    }
}
