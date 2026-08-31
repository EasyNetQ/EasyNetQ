namespace EasyNetQ.Configuration;

/// <summary>
///     RabbitMQ-typed configuration root: <c>bus.UseRabbitMq(r =&gt; r.Consume(...))</c>. The transport owns the
///     typed lower layers; the generic top-level API remains for portable code.
/// </summary>
public sealed class RabbitMqBuilder
{
    private readonly IEasyNetQBuilder builder;

    internal RabbitMqBuilder(IEasyNetQBuilder builder) => this.builder = builder;

    /// <summary>
    ///     Registers a consumer with RabbitMQ-typed queue, exchange and consumer settings
    /// </summary>
    public RabbitMqBuilder Consume(Action<RabbitMqConsumerBuilder> configure)
    {
        builder.RegisterConsumer(new RabbitMqConsumerBuilder(new ConsumerDefinition()), configure);
        return this;
    }
}

/// <summary>
///     Entry point of the RabbitMQ-typed fluent configuration
/// </summary>
public static class EasyNetQBuilderRabbitMqExtensions
{
    /// <summary>
    ///     Configures the bus with RabbitMQ-typed builders
    /// </summary>
    public static IEasyNetQBuilder UseRabbitMq(this IEasyNetQBuilder builder, Action<RabbitMqBuilder> configure)
    {
        configure(new RabbitMqBuilder(builder));
        return builder;
    }
}
