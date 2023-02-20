using EasyNetQ.DI;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer;

public sealed class ConsumeServiceScopeMiddleware : IConsumeMiddleware
{
    private readonly IServiceResolver resolver;

    public ConsumeServiceScopeMiddleware(IServiceResolver resolver) => this.resolver = resolver;

    public async ValueTask<AckStrategy> InvokeAsync(ConsumeContext context, ConsumeDelegate next)
    {
        await using var _ = new AsyncServiceResolverScope(resolver.CreateScope());

        return await next(context).ConfigureAwait(false);
    }
}

public sealed class ConsumeErrorHandlingMiddleware : IConsumeMiddleware
{
    private readonly ILogger<ConsumeErrorHandlingMiddleware> logger;
    private readonly IConsumeErrorStrategy errorStrategy;

    public ConsumeErrorHandlingMiddleware(ILogger<ConsumeErrorHandlingMiddleware> logger, IConsumeErrorStrategy errorStrategy)
    {
        this.logger = logger;
        this.errorStrategy = errorStrategy;
    }

    public async ValueTask<AckStrategy> InvokeAsync(ConsumeContext context, ConsumeDelegate next)
    {
        try
        {
            try
            {
                return await next(context).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return await errorStrategy.HandleCancelledAsync(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return await errorStrategy.HandleErrorAsync(context, exception).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Consumer error strategy has failed");
            return AckStrategies.NackWithRequeue;
        }
    }
}
