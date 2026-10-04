using System.Collections.Concurrent;
using System.Diagnostics;
using EasyNetQ.Diagnostics;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Starts the "publish {exchange}" PRODUCER span around each publish and injects the trace context into the
///     outgoing message headers (per <see cref="TelemetryOptions.PropagateContext" />). The RabbitMQ client's
///     own publisher span (when its source is enabled) nests inside this one and performs its own injection,
///     which then wins - this injection matters when the client's source is not enabled. With no listeners and
///     Auto propagation with no current activity, it is a single check and a tail call.
/// </summary>
public sealed class PublishTracingMiddleware : IMiddleware<PublishContext>
{
    private static readonly DistributedContextPropagator.PropagatorSetterCallback HeaderSetter =
        static (object? carrier, string fieldName, string fieldValue) =>
        {
            var context = (PublishContext)carrier!;
            context.Properties = context.Properties.SetHeader(fieldName, fieldValue);
        };

    // exchanges are a small, bounded set in practice; cap defensively so a pathological caller cannot leak
    private static readonly ConcurrentDictionary<string, string> SpanNames = new();

    private readonly TelemetryOptions options;

    /// <summary>
    ///     Creates the middleware
    /// </summary>
    public PublishTracingMiddleware(TelemetryOptions options) => this.options = options;

    /// <inheritdoc />
    public ValueTask InvokeAsync(PublishContext context, PipelineStep<PublishContext> next)
    {
        var hasListeners = EasyNetQDiagnostics.Source.HasListeners();
        if (!hasListeners && (options.PropagateContext == ContextPropagationMode.Never
                              || (options.PropagateContext == ContextPropagationMode.Auto && Activity.Current is null)))
            return next(context);

        return TraceAsync(context, next, hasListeners);
    }

    private async ValueTask TraceAsync(PublishContext context, PipelineStep<PublishContext> next, bool hasListeners)
    {
        var activity = hasListeners
            ? EasyNetQDiagnostics.Source.StartActivity(SpanName(context.Exchange), ActivityKind.Producer)
            : null;

        if (activity is not null)
        {
            activity.SetTag(MessagingTags.MessagingSystem, options.MessagingSystem);
            activity.SetTag(MessagingTags.OperationType, "send");
            activity.SetTag(MessagingTags.OperationName, "publish");
            activity.SetTag(MessagingTags.DestinationName, context.Exchange);
            if (options.RecordRoutingKey)
                activity.SetTag(MessagingTags.RabbitMqRoutingKey, context.RoutingKey);
            if (context.Properties.MessageIdPresent)
                activity.SetTag(MessagingTags.MessageId, context.Properties.MessageId);
            if (context.Properties.CorrelationIdPresent)
                activity.SetTag(MessagingTags.ConversationId, context.Properties.CorrelationId);
        }

        if (options.PropagateContext != ContextPropagationMode.Never && (activity ?? Activity.Current) is { } toInject)
            DistributedContextPropagator.Current.Inject(toInject, context, HeaderSetter);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (activity is not null)
            {
                activity.SetTag(MessagingTags.ErrorType, exception.GetType().FullName);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            throw;
        }
        finally
        {
            if (activity is not null)
            {
                if (options.RecordMessageType && context.Properties.TypePresent)
                    activity.SetTag(MessagingTags.MessageType, context.Properties.Type);
                if (options.RecordBodySize)
                    activity.SetTag(MessagingTags.BodySize, context.Body.Length);
                activity.Dispose();
            }
        }
    }

    private static string SpanName(string exchange)
    {
        if (SpanNames.TryGetValue(exchange, out var name)) return name;
        name = $"publish {exchange}";
        if (SpanNames.Count < 1000) SpanNames.TryAdd(exchange, name);
        return name;
    }
}
