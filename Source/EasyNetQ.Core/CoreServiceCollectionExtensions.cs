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
        services.TryAddSingleton<IConventions, Conventions>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.TryAddSingleton<ITypeNameSerializer, DefaultTypeNameSerializer>();
        services.TryAddSingleton<PipelineBuilder<PublishContext>>(_ => new PipelineBuilder<PublishContext>().UseProduceInterceptors());
        services.TryAddSingleton<PipelineBuilder<ConsumeContext>>(_ =>
            new PipelineBuilder<ConsumeContext>().UseConsumeErrorStrategy().UseConsumeInterceptors());
        services.TryAddSingleton<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>();
        services.TryAddSingleton<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>();
        services.TryAddSingleton<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>();
        services.TryAddSingleton<IHandlerCollectionFactory, HandlerCollectionFactory>();
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        return services;
    }
}
