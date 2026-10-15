using System.Diagnostics.CodeAnalysis;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Resolves the incoming wire type name (the AMQP "type" property) to a <see cref="MessageTypeDescriptor" />
///     via the consumer's <see cref="HandlerTable" />
/// </summary>
public sealed class ResolveMessageTypeStep : IMiddleware<ConsumeContext>
{
    // written by UseVersionedMessage publishers: the older versions (ISupersede<T>) the message also satisfies
    private const string AlternativeMessageTypesHeader = "Alternative-Message-Types";

    /// <inheritdoc />
    public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        var handlers = context.RequireHandlers();
        var properties = context.Properties;
        context.MessageType = properties.HeadersPresent && TryResolveVersioned(handlers, properties, out var versioned)
            ? versioned
            : handlers.ResolveDescriptor(properties.Type);
        return next(context);
    }

    // a newer version this consumer cannot load falls back to the first alternative it can, as 8.x did
    private static bool TryResolveVersioned(HandlerTable handlers, in MessageProperties properties, [MaybeNullWhen(false)] out MessageTypeDescriptor descriptor)
    {
        descriptor = null;
        if (properties.Headers?.TryGetValue(AlternativeMessageTypesHeader, out var header) != true || header is not byte[] alternatives)
            return false;
        if (properties.Type is { Length: > 0 } type && handlers.TryResolveDescriptor(type, out descriptor))
            return true;
        foreach (var alternative in System.Text.Encoding.UTF8.GetString(alternatives).Split([';'], StringSplitOptions.RemoveEmptyEntries))
            if (handlers.TryResolveDescriptor(alternative, out descriptor))
                return true;
        return false;
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
        context.Handler = context.RequireHandlers().Resolve(context.RequireMessageType());
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
        // the unknown-message handler reads the raw body; nothing to deserialize
        context.Message = context.Body.IsEmpty || context.Handler is RawHandlerEntry
            ? null
            : context.RequireMessageType().DeserializeBody(context.RequireSerializer(), context.Body);
        return next(context);
    }
}
