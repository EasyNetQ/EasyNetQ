namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Resolves the incoming wire type name (the AMQP "type" property) to a <see cref="MessageTypeDescriptor" />
///     via the consumer's <see cref="HandlerTable" />
/// </summary>
public sealed class ResolveMessageTypeStep : IMiddleware<ConsumeContext>
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        context.MessageType = context.Consumer.Handlers!.ResolveDescriptor(context.Properties.Type);
        return next(context);
    }
}

/// <summary>
///     Resolves the handler for the message type (exact or polymorphic match)
/// </summary>
public sealed class ResolveHandlerStep : IMiddleware<ConsumeContext>
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        context.Handler = context.Consumer.Handlers!.Resolve(context.MessageType!);
        return next(context);
    }
}

/// <summary>
///     Picks the serializer for this message: the nearest <see cref="Keys.Serializer" /> up the context hierarchy,
///     falling back to the bus default
/// </summary>
public sealed class SelectSerializerStep : IMiddleware<ConsumeContext>
{
    private readonly IMessageSerializer defaultSerializer;

    /// <summary>
    ///     Creates the step with the bus-wide default serializer
    /// </summary>
    public SelectSerializerStep(IMessageSerializer defaultSerializer)
    {
        this.defaultSerializer = defaultSerializer;
    }

    /// <inheritdoc />
    public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        context.Serializer = context.TryGet(Keys.Serializer, out var serializer) ? serializer : defaultSerializer;
        return next(context);
    }
}

/// <summary>
///     Deserializes the body into <see cref="ConsumeContext.Message" /> using the resolved descriptor and serializer.
///     Middleware between this step and dispatch can inspect or replace the deserialized message.
/// </summary>
public sealed class DeserializeStep : IMiddleware<ConsumeContext>
{
    /// <inheritdoc />
    public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        context.Message = context.Body.IsEmpty
            ? null
            : context.MessageType!.DeserializeBody(context.Serializer!, context.Body);
        return next(context);
    }
}
