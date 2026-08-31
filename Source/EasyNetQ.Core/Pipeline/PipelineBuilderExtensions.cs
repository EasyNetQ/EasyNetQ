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
    /// <summary>
    ///     Appends the consume metrics step (outermost by default so error handling never hides a delivery)
    /// </summary>
    public static PipelineBuilder<ConsumeContext> UseConsumeMetrics(this PipelineBuilder<ConsumeContext> builder)
        => builder.Use<Middleware.ConsumeMetricsMiddleware>();

    /// <summary>
    ///     Appends the consume tracing step (inside error handling so failures mark the span)
    /// </summary>
    public static PipelineBuilder<ConsumeContext> UseConsumeTracing(this PipelineBuilder<ConsumeContext> builder)
        => builder.Use<Middleware.ConsumeTracingMiddleware>();

    /// <summary>
    ///     Appends the publish metrics step
    /// </summary>
    public static PipelineBuilder<PublishContext> UsePublishMetrics(this PipelineBuilder<PublishContext> builder)
        => builder.Use<Middleware.PublishMetricsMiddleware>();

    /// <summary>
    ///     Appends the publish tracing and context-propagation step
    /// </summary>
    public static PipelineBuilder<PublishContext> UsePublishTracing(this PipelineBuilder<PublishContext> builder)
        => builder.Use<Middleware.PublishTracingMiddleware>();

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

    /// <summary>
    ///     Appends the typed dispatch steps of the consume pipeline: resolve message type and handler, select the
    ///     serializer, deserialize
    /// </summary>
    public static PipelineBuilder<ConsumeContext> UseTypedDispatch(this PipelineBuilder<ConsumeContext> builder, IMessageSerializer defaultSerializer)
        => builder
            .Use(new ResolveMessageTypeStep())
            .Use(new ResolveHandlerStep())
            .Use(new SelectSerializerStep(defaultSerializer))
            .Use(new DeserializeStep());

    /// <summary>
    ///     Inserts <paramref name="serializeStep" /> before the produce interceptors so they see the serialized
    ///     body, or appends it when no interceptor step is registered
    /// </summary>
    public static PipelineBuilder<PublishContext> UseSerialize(this PipelineBuilder<PublishContext> builder, SerializeStep serializeStep)
        => builder.Contains<ProduceInterceptorMiddleware>()
            ? builder.InsertBefore<ProduceInterceptorMiddleware>(serializeStep)
            : builder.Use(serializeStep);
}
