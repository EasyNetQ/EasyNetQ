using System.Diagnostics;
using System.Text;
using EasyNetQ.Diagnostics;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Starts the "process {queue}" CONSUMER span around message processing. Runs inside error handling so a
///     failed message marks the span before the error strategy turns the exception into an ack decision. The
///     RabbitMQ client's deliver span (when its source is enabled) is Activity.Current here and becomes the
///     parent; otherwise the parent is extracted from the message headers (traceparent/tracestate, string or
///     byte[] values). With no listeners on the EasyNetQ source it is a single check and a tail call.
/// </summary>
public sealed class ConsumeTracingMiddleware : IMiddleware<ConsumeContext>
{
    private static readonly DistributedContextPropagator.PropagatorGetterCallback HeaderGetter =
        static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
        {
            fieldValues = null;
            fieldValue = null;
            var headers = ((ConsumeContext)carrier!).Properties.Headers;
            if (headers != null && headers.TryGetValue(fieldName, out var raw))
                fieldValue = raw switch
                {
                    string stringValue => stringValue,
                    byte[] bytesValue => Encoding.UTF8.GetString(bytesValue),
                    _ => null
                };
        };

    private readonly TelemetryOptions options;

    /// <summary>
    ///     Creates the middleware
    /// </summary>
    public ConsumeTracingMiddleware(TelemetryOptions options) => this.options = options;

    /// <inheritdoc />
    public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        if (!EasyNetQDiagnostics.Source.HasListeners())
            return next(context);

        return TraceAsync(context, next);
    }

    private async ValueTask TraceAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        context.TryGet(Keys.ConsumerTelemetry, out var telemetry);
        var name = telemetry?.SpanName ?? $"process {context.ReceivedInfo.Queue}";

        var parent = default(ActivityContext);
        if (Activity.Current is null)
        {
            DistributedContextPropagator.Current.ExtractTraceIdAndState(context, HeaderGetter, out var traceParent, out var traceState);
            if (traceParent != null)
                ActivityContext.TryParse(traceParent, traceState, isRemote: true, out parent);
        }

        using var activity = EasyNetQDiagnostics.Source.StartActivity(name, ActivityKind.Consumer, parent);
        if (activity is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        activity.SetTag(MessagingTags.MessagingSystem, telemetry?.MessagingSystem ?? options.MessagingSystem);
        activity.SetTag(MessagingTags.OperationType, "process");
        activity.SetTag(MessagingTags.OperationName, "process");
        activity.SetTag(MessagingTags.DestinationSubscriptionName, telemetry?.Queue ?? context.ReceivedInfo.Queue);
        activity.SetTag(MessagingTags.DestinationName, context.ReceivedInfo.Exchange);
        if (options.RecordRoutingKey)
            activity.SetTag(MessagingTags.RabbitMqRoutingKey, context.ReceivedInfo.RoutingKey);
        if (context.Properties.MessageIdPresent)
            activity.SetTag(MessagingTags.MessageId, context.Properties.MessageId);
        if (context.Properties.CorrelationIdPresent)
            activity.SetTag(MessagingTags.ConversationId, context.Properties.CorrelationId);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            activity.SetTag(MessagingTags.ErrorType, exception.GetType().FullName);
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
        finally
        {
            if (options.RecordMessageType && context.MessageType is { } messageType)
                activity.SetTag(MessagingTags.MessageType, messageType.DisplayName);
            activity.SetTag(MessagingTags.AckDecision, TelemetryValues.AckName(context.Ack));
            if (context.Error is { } error && activity.Status != ActivityStatusCode.Error)
            {
                activity.SetTag(MessagingTags.ErrorType, error.GetType().FullName);
                activity.SetStatus(ActivityStatusCode.Error, error.Message);
            }
        }
    }
}
