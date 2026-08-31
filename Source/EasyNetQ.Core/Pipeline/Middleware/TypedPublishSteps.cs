namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Serializes <see cref="PublishContext.Message" /> into the body and stamps the wire type name, correlation id
///     and delivery mode. Skips contexts publishing a pre-serialized body. Middleware after this step sees the
///     serialized body (e.g. to compress or encrypt it).
/// </summary>
public sealed class SerializeStep : IMiddleware<PublishContext>
{
    private readonly IMessageSerializer defaultSerializer;
    private readonly ICorrelationIdGenerationStrategy correlationIdGenerator;
    private readonly bool persistentMessagesByDefault;

    /// <summary>
    ///     Creates the step with the bus-wide defaults
    /// </summary>
    public SerializeStep(
        IMessageSerializer defaultSerializer,
        ICorrelationIdGenerationStrategy correlationIdGenerator,
        bool persistentMessagesByDefault
    )
    {
        this.defaultSerializer = defaultSerializer;
        this.correlationIdGenerator = correlationIdGenerator;
        this.persistentMessagesByDefault = persistentMessagesByDefault;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(PublishContext context, PipelineStep<PublishContext> next)
    {
        var descriptor = context.MessageType;
        if (descriptor is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var serializer = context.Serializer
            ?? (context.TryGet(Keys.Serializer, out var scoped) ? scoped : defaultSerializer);
        context.Serializer = serializer;

        var properties = context.Properties;
        properties = properties with
        {
            Type = properties.TypePresent ? properties.Type : descriptor.WireName,
            CorrelationId = string.IsNullOrEmpty(properties.CorrelationId)
                ? correlationIdGenerator.GetCorrelationId()
                : properties.CorrelationId,
        };
        if (!properties.DeliveryModePresent)
            properties = properties with
            {
                DeliveryMode = descriptor.IsPersistent ?? persistentMessagesByDefault
                    ? MessageDeliveryMode.Persistent
                    : MessageDeliveryMode.NonPersistent
            };
        context.Properties = properties;

        if (context.Message is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        using var body = descriptor.SerializeBody(serializer, context.Message);
        context.Body = body.Memory;
        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Body = default;
        }
    }
}
