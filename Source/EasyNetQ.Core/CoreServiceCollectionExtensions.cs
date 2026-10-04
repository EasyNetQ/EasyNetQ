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
        services.TryAddSingleton<BusOptions>(_ => new BusOptions());
        services.TryAddSingleton<IMessageTypeRegistry>(sp => new MessageTypeRegistry(
            sp.GetRequiredService<ITypeNameSerializer>(),
            sp.GetServices<IMessageTypeRegistryInitializer>(),
            sp.GetServices<MessageTypeMapping>()
        ));
        services.TryAddSingleton<IMessageSerializer>(sp =>
        {
            if (sp.GetService<ISerializer>() is { } legacySerializer)
                return new Serialization.LegacyMessageSerializerAdapter(legacySerializer);

            // the transport's and the application's source-generated contexts first; reflection only where it works
            var contexts = sp.GetServices<System.Text.Json.Serialization.JsonSerializerContext>();
            return new Serialization.SystemTextJson.SystemTextJsonMessageSerializer(
                Serialization.SystemTextJson.SystemTextJsonMessageSerializer.CreateDefaultResolver(contexts),
                sp.GetServices<System.Text.Json.Serialization.JsonConverter>()
            );
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
        services.TryAddSingleton<IRpc, TransportRpc>();
        services.TryAddSingleton<PipelineBuilder<LifecycleContext>>(_ => new PipelineBuilder<LifecycleContext>());
        services.TryAddSingleton<LifecycleNotifier>();
        services.TryAddSingleton<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>();
        services.TryAddSingleton<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>();
        services.TryAddSingleton<IHandlerCollectionFactory, HandlerCollectionFactory>();
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        return services;
    }
}
