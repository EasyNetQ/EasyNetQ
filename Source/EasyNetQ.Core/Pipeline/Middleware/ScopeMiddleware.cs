using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Creates a service scope for the duration of a message so scoped services can be resolved from
///     <see cref="LayerContext.Services" />
/// </summary>
public sealed class ScopeMiddleware : IMiddleware<ConsumeContext>
{
    /// <inheritdoc />
#if NET
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
#endif
    public async ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        var services = context.Services;
        await using var scope = services.CreateAsyncScope();
        context.Services = scope.ServiceProvider;
        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Services = services;
        }
    }
}
