using EasyNetQ.Consumer;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    ///     Declare the error queue with typed settings, e.g. <c>ErrorQueue(q =&gt; q.Quorum())</c> so failed messages
    ///     are replicated on a cluster. One error queue serves every consumer, so this is bus-wide.
    /// </summary>
    public RabbitMqBuilder ErrorQueue(Action<RabbitMqQueueBuilder> configure)
    {
        var queueBuilder = new RabbitMqQueueBuilder();
        configure(queueBuilder);
        ErrorOptions().ErrorQueueArguments = queueBuilder.Build("").Arguments;
        return this;
    }

    /// <summary>
    ///     Log failed message bodies next to the error (off by default; the error queue keeps them)
    /// </summary>
    public RabbitMqBuilder LogFailedMessageBodies(bool enabled = true)
    {
        ErrorOptions().LogMessageBody = enabled;
        return this;
    }

    private ConsumeErrorOptions ErrorOptions()
    {
        if (builder.Services.LastOrDefault(d => d.ServiceType == typeof(ConsumeErrorOptions))?.ImplementationInstance is ConsumeErrorOptions existing)
            return existing;
        var options = new ConsumeErrorOptions();
        builder.Services.AddSingleton(options);
        return options;
    }

    /// <summary>
    ///     Registers a publish definition with RabbitMQ-typed exchange settings, used by
    ///     <see cref="IMessagePublisher" />
    /// </summary>
    public RabbitMqBuilder Publish(Action<RabbitMqPublishBuilder> configure)
    {
        builder.RegisterPublisher(new RabbitMqPublishBuilder(new PublishDefinition()), configure);
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
