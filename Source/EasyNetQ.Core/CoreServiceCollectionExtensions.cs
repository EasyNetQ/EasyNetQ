using EasyNetQ.Consumer;
using EasyNetQ.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ;

public static class CoreServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the transport-agnostic core services (registry, serialization, pipelines, facades).
    /// </summary>
    /// <summary>
    ///     Registers the transport-agnostic services and returns the fluent builder. Register an
    ///     <see cref="Transport.ITransport" /> implementation separately (a transport package does this).
    /// </summary>
    public static IEasyNetQBuilder AddEasyNetQCore(this IServiceCollection services)
    {
        services.AddEasyNetQCoreServices();
        return new EasyNetQBuilder(services);
    }

    public static IServiceCollection AddEasyNetQCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessageTypeRegistry, MessageTypeRegistry>();
        services.TryAddSingleton<IMessageSerializer>(sp =>
        {
            if (sp.GetService<ISerializer>() is { } legacySerializer)
                return new Serialization.LegacyMessageSerializerAdapter(legacySerializer);

            // Source-generated modules register JsonSerializerContexts; combining them keeps serialization
            // reflection-free (and AOT-safe) for every discovered message type
            var contexts = sp.GetServices<System.Text.Json.Serialization.JsonSerializerContext>().ToArray();
            var converters = sp.GetServices<System.Text.Json.Serialization.JsonConverter>();
            return contexts.Length == 0
                ? new Serialization.SystemTextJson.SystemTextJsonMessageSerializer(
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.General), converters)
                : new Serialization.SystemTextJson.SystemTextJsonMessageSerializer(
                    System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine(contexts), converters);
        });
        services.TryAddSingleton<Consumer.IConsumeErrorStrategy>(Consumer.SimpleConsumeErrorStrategy.NackWithRequeue);
        services.TryAddSingleton<IConventions, Conventions>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.TryAddSingleton<ITypeNameSerializer, DefaultTypeNameSerializer>();
        services.TryAddSingleton<Diagnostics.TelemetryOptions>();
        services.TryAddSingleton<Pipeline.Middleware.PublishMetricsMiddleware>();
        services.TryAddSingleton<Pipeline.Middleware.PublishTracingMiddleware>();
        services.TryAddSingleton<Pipeline.Middleware.ConsumeMetricsMiddleware>();
        services.TryAddSingleton<Pipeline.Middleware.ConsumeTracingMiddleware>();
        services.TryAddSingleton<PipelineBuilder<PublishContext>>(_ =>
            new PipelineBuilder<PublishContext>().UsePublishMetrics().UsePublishTracing().UseProduceInterceptors());
        services.TryAddSingleton<PipelineBuilder<ConsumeContext>>(_ =>
            new PipelineBuilder<ConsumeContext>().UseConsumeMetrics().UseConsumeErrorStrategy().UseConsumeTracing().UseConsumeInterceptors());
        services.TryAddSingleton<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>();
        services.TryAddSingleton<IMessagePublisher, TransportMessagePublisher>();
        services.TryAddSingleton<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>();
        services.TryAddSingleton<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>();
        services.TryAddSingleton<IHandlerCollectionFactory, HandlerCollectionFactory>();
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        return services;
    }
}
