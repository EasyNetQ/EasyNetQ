using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Turns exceptions thrown further down the consume pipeline into an <see cref="AckDecision" /> via the
///     configured <see cref="IConsumeErrorStrategy" />. Should be the outermost consume step.
/// </summary>
public sealed class ErrorHandlingMiddleware : IMiddleware<ConsumeContext>
{
    // bounded: a producer sending endless distinct type names must not grow this without limit
    private const int MaxReportedUnknownTypes = 1024;

    private readonly IConsumeErrorStrategy errorStrategy;
    private readonly ILogger<ErrorHandlingMiddleware> logger;
    private readonly ConcurrentDictionary<(string Queue, string WireName), bool> reportedUnknownTypes = new();

    /// <summary>
    ///     Creates the middleware
    /// </summary>
    public ErrorHandlingMiddleware(IConsumeErrorStrategy errorStrategy, ILogger<ErrorHandlingMiddleware> logger)
    {
        this.errorStrategy = errorStrategy;
        this.logger = logger;
    }

    /// <inheritdoc />
#if NET
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
#endif
    public async ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        try
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                context.Ack = await errorStrategy.HandleCancelledAsync(context, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (exception is UnknownMessageTypeException unknown)
                    ReportUnknownType(context, unknown);
                context.Error = exception;
                context.Ack = await errorStrategy.HandleErrorAsync(context, exception, context.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            logger.ConsumeErrorStrategyFailed(exception);
            context.Ack = AckDecision.NackRequeue;
        }
    }

    private void ReportUnknownType(ConsumeContext context, UnknownMessageTypeException exception)
    {
        var key = (context.ReceivedInfo.Queue, exception.WireName ?? "");
        if (reportedUnknownTypes.Count < MaxReportedUnknownTypes && reportedUnknownTypes.TryAdd(key, true))
            logger.UnknownMessageType(context.ReceivedInfo.Queue, exception.WireName, exception.Message);
    }
}
