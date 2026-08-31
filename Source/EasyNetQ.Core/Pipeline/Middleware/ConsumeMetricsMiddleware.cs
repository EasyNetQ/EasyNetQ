using System.Diagnostics;
using EasyNetQ.Diagnostics;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Records messaging.client.consumed.messages, messaging.process.duration, easynetq.consumer.messages and
///     easynetq.consumer.in_flight around each delivery. Outermost consume step, so sampling and error handling
///     never affect the numbers; with no meter listeners it is a single check and a tail call.
/// </summary>
public sealed class ConsumeMetricsMiddleware : IMiddleware<ConsumeContext>
{
    private readonly TelemetryOptions options;

    /// <summary>
    ///     Creates the middleware
    /// </summary>
    public ConsumeMetricsMiddleware(TelemetryOptions options) => this.options = options;

    /// <inheritdoc />
    public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        if (!EasyNetQDiagnostics.ConsumedMessages.Enabled
            && !EasyNetQDiagnostics.ProcessDuration.Enabled
            && !EasyNetQDiagnostics.ConsumerMessages.Enabled
            && !EasyNetQDiagnostics.ConsumerInFlight.Enabled)
            return next(context);

        return MeasureAsync(context, next);
    }

    private async ValueTask MeasureAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        context.TryGet(Keys.ConsumerTelemetry, out var telemetry);
        var system = telemetry?.MessagingSystem ?? options.MessagingSystem;
        var queue = telemetry?.Queue ?? context.ReceivedInfo.Queue;

        EasyNetQDiagnostics.ConsumerInFlight.Add(1);
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            EasyNetQDiagnostics.ConsumerInFlight.Add(-1);

            var tags = new TagList
            {
                { MessagingTags.MessagingSystem, system },
                { MessagingTags.DestinationSubscriptionName, queue }
            };
            if (options.RecordMessageType && context.MessageType is { } messageType)
                tags.Add(MessagingTags.MessageType, messageType.DisplayName);
            if (context.Error is { } error)
                tags.Add(MessagingTags.ErrorType, error.GetType().FullName);

            EasyNetQDiagnostics.ConsumedMessages.Add(1, in tags);
            EasyNetQDiagnostics.ProcessDuration.Record(elapsed.TotalSeconds, in tags);

            tags.Add(MessagingTags.AckDecision, TelemetryValues.AckName(context.Ack));
            EasyNetQDiagnostics.ConsumerMessages.Add(1, in tags);
        }
    }
}

internal static class TelemetryValues
{
    internal static string AckName(AckDecision decision) => decision switch
    {
        AckDecision.Ack => "ack",
        AckDecision.NackRequeue => "nack_requeue",
        AckDecision.NackDiscard => "nack_discard",
        AckDecision.Handled => "handled",
        _ => "unknown"
    };
}
