using System.Runtime.CompilerServices;
using EasyNetQ.Consumer;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Turns exceptions thrown further down the consume pipeline into an <see cref="AckDecision" /> via the
///     configured <see cref="IConsumeErrorStrategy" />. Should be the outermost consume step.
/// </summary>
public sealed class ErrorHandlingMiddleware : IMiddleware<ConsumeContext>
{
    private readonly IConsumeErrorStrategy errorStrategy;
    private readonly ILogger<ErrorHandlingMiddleware> logger;

    /// <summary>
    ///     Creates the middleware
    /// </summary>
    public ErrorHandlingMiddleware(IConsumeErrorStrategy errorStrategy, ILogger<ErrorHandlingMiddleware> logger)
    {
        this.errorStrategy = errorStrategy;
        this.logger = logger;
    }

    /// <inheritdoc />
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
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
                context.Error = exception;
                context.Ack = await errorStrategy.HandleErrorAsync(context, exception, context.CancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Consume error strategy has failed");
            context.Ack = AckDecision.NackRequeue;
        }
    }
}
