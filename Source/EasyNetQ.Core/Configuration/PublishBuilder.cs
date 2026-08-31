using EasyNetQ.Pipeline;
using EasyNetQ.Transport;

namespace EasyNetQ.Configuration;

/// <summary>
///     Base of the fluent publish builders. Transports derive typed builders
///     (<c>UseRabbitMq(r =&gt; r.Publish(p =&gt; p.Exchange("orders", e =&gt; e.Topic())))</c>); the shared members
///     return <typeparamref name="TSelf" /> so a chain never degrades to the base type.
/// </summary>
public abstract class PublishBuilder<TSelf> where TSelf : PublishBuilder<TSelf>
{
    /// <summary>
    ///     Creates a builder over <paramref name="definition" />
    /// </summary>
    protected PublishBuilder(PublishDefinition definition) => Definition = definition;

    /// <summary>
    ///     The definition being built
    /// </summary>
    public PublishDefinition Definition { get; }

    private TSelf Self => (TSelf)this;

    /// <summary>
    ///     Publish to <paramref name="name" />, declaring it before the first publish
    /// </summary>
    public TSelf Exchange(string name)
    {
        Definition.Exchange = name;
        Definition.ExchangeToDeclare = new ExchangeDefinition(name);
        return Self;
    }

    /// <summary>
    ///     Publish to <paramref name="name" /> without declaring it. The default exchange is <c>""</c>.
    /// </summary>
    public TSelf ExistingExchange(string name)
    {
        Definition.Exchange = name;
        Definition.ExchangeToDeclare = null;
        return Self;
    }

    /// <summary>
    ///     Route <typeparamref name="T" /> through this definition with an empty routing key
    /// </summary>
    public TSelf Message<T>() => Message<T>("");

    /// <summary>
    ///     Route <typeparamref name="T" /> through this definition with a fixed routing key
    /// </summary>
    public TSelf Message<T>(string routingKey)
    {
        Definition.MessageRegistrations.Add(table => table.Add<T>(Definition, routingKey));
        return Self;
    }

    /// <summary>
    ///     Route <typeparamref name="T" /> through this definition with a per-message routing key
    /// </summary>
    public TSelf Message<T>(Func<T, string> routingKey)
    {
        Definition.MessageRegistrations.Add(table => table.Add(Definition, routingKey));
        return Self;
    }

    /// <summary>
    ///     Broker must route every message to at least one queue
    /// </summary>
    public TSelf Mandatory(bool mandatory = true)
    {
        Definition.Mandatory = mandatory;
        return Self;
    }

    /// <summary>
    ///     Wait for a broker confirmation on every publish
    /// </summary>
    public TSelf PublisherConfirms(bool publisherConfirms = true)
    {
        Definition.PublisherConfirms = publisherConfirms;
        return Self;
    }

    /// <summary>
    ///     Customize the publish pipeline for this definition (runs after <see cref="Pipeline.Middleware.SerializeStep" />
    ///     is in place, so InsertBefore/InsertAfter can target it)
    /// </summary>
    public TSelf Pipeline(Action<PipelineBuilder<PublishContext>> configure)
    {
        Definition.MessagePipeline += configure;
        return Self;
    }
}

/// <summary>
///     The transport-agnostic publish builder
/// </summary>
public sealed class GenericPublishBuilder : PublishBuilder<GenericPublishBuilder>
{
    /// <summary>
    ///     Creates the builder
    /// </summary>
    public GenericPublishBuilder(PublishDefinition definition) : base(definition)
    {
    }
}
