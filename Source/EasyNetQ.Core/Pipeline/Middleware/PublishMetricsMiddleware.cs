using System.Diagnostics;
using EasyNetQ.Diagnostics;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Records messaging.client.sent.messages and messaging.client.operation.duration around each publish.
///     Outermost publish step; with no meter listeners it is a single check and a tail call.
/// </summary>
public sealed class PublishMetricsMiddleware : IMiddleware<PublishContext>
{
    private readonly TelemetryOptions options;

    /// <summary>
    ///     Creates the middleware
    /// </summary>
    public PublishMetricsMiddleware(TelemetryOptions options) => this.options = options;

    /// <inheritdoc />
    public ValueTask InvokeAsync(PublishContext context, PipelineStep<PublishContext> next)
    {
        if (!EasyNetQDiagnostics.SentMessages.Enabled && !EasyNetQDiagnostics.OperationDuration.Enabled)
            return next(context);

        return MeasureAsync(context, next);
    }

    private async ValueTask MeasureAsync(PublishContext context, PipelineStep<PublishContext> next)
    {
        var startedAt = Stopwatch.GetTimestamp();
        Exception? failure = null;
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var tags = new TagList
            {
                { MessagingTags.MessagingSystem, options.MessagingSystem },
                { MessagingTags.OperationName, "publish" },
                { MessagingTags.DestinationName, context.Exchange }
            };
            if (options.RecordMessageType && context.Properties.TypePresent)
                tags.Add(MessagingTags.MessageType, context.Properties.Type);
            if (failure != null)
                tags.Add(MessagingTags.ErrorType, failure.GetType().FullName);

            EasyNetQDiagnostics.SentMessages.Add(1, in tags);
            EasyNetQDiagnostics.OperationDuration.Record(elapsed.TotalSeconds, in tags);
        }
    }
}
