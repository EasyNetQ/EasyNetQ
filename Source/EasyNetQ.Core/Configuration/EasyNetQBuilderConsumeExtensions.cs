using EasyNetQ.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Configuration;

/// <summary>
///     Fluent consumer registration
/// </summary>
public static class EasyNetQBuilderConsumeExtensions
{
    /// <summary>
    ///     Registers a transport-agnostic consumer, started by the host
    /// </summary>
    public static IEasyNetQBuilder Consume(this IEasyNetQBuilder builder, Action<GenericConsumerBuilder> configure)
        => builder.RegisterConsumer(new GenericConsumerBuilder(new ConsumerDefinition()), configure);

    /// <summary>
    ///     Registers a consumer built by a transport-typed builder. Transports call this from their own fluent
    ///     entry points.
    /// </summary>
    public static IEasyNetQBuilder RegisterConsumer<TBuilder>(this IEasyNetQBuilder builder, TBuilder consumerBuilder, Action<TBuilder> configure)
        where TBuilder : ConsumerBuilder<TBuilder>
    {
        configure(consumerBuilder);
        builder.Services.AddSingleton(consumerBuilder.Definition);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ConsumerHostedService>());
        return builder;
    }
}
