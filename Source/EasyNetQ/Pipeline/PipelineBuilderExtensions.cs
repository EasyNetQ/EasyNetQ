using EasyNetQ.Interception;
using EasyNetQ.Pipeline.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Pipeline;

/// <summary>
///     Registration helpers for the built-in middleware
/// </summary>
public static class PipelineBuilderExtensions
{
    /// <summary>
    ///     Adds <see cref="ErrorHandlingMiddleware" />
    /// </summary>
    public static PipelineBuilder<ConsumeContext> UseConsumeErrorStrategy(this PipelineBuilder<ConsumeContext> builder)
        => builder.Use(static services => new ErrorHandlingMiddleware(
            services.GetRequiredService<Consumer.IConsumeErrorStrategy>(),
            services.GetRequiredService<ILogger<ErrorHandlingMiddleware>>()
        ));

    /// <summary>
    ///     Adds <see cref="ConsumeInterceptorMiddleware" />
    /// </summary>
    public static PipelineBuilder<ConsumeContext> UseConsumeInterceptors(this PipelineBuilder<ConsumeContext> builder)
        => builder.Use(static services => new ConsumeInterceptorMiddleware(services.GetServices<IProduceConsumeInterceptor>()));

    /// <summary>
    ///     Adds <see cref="ScopeMiddleware" />
    /// </summary>
    public static PipelineBuilder<ConsumeContext> UseScope(this PipelineBuilder<ConsumeContext> builder)
        => builder.Use(new ScopeMiddleware());

    /// <summary>
    ///     Adds <see cref="ProduceInterceptorMiddleware" />
    /// </summary>
    public static PipelineBuilder<PublishContext> UseProduceInterceptors(this PipelineBuilder<PublishContext> builder)
        => builder.Use(static services => new ProduceInterceptorMiddleware(services.GetServices<IProduceConsumeInterceptor>()));
}
