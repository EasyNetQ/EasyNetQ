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
        AddConsumerHostStatus(builder.Services);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ConsumerHostedService>());
        return builder;
    }

    /// <summary>
    ///     Configures how the consumer host starts the consumers (background start with retries by default)
    /// </summary>
    public static IEasyNetQBuilder ConsumerHost(this IEasyNetQBuilder builder, Action<ConsumerHostOptions> configure)
    {
        configure(HostOptions(builder.Services));
        return builder;
    }

    /// <summary>Options and status shared by every consumer host (fluent consumers, auto-subscriber)</summary>
    internal static void AddConsumerHostStatus(IServiceCollection services)
    {
        HostOptions(services);
        services.TryAddSingleton<ConsumerHostStatus>();
        services.TryAddSingleton<IConsumerHostStatus>(sp => sp.GetRequiredService<ConsumerHostStatus>());
    }

    private static ConsumerHostOptions HostOptions(IServiceCollection services)
    {
        if (services.LastOrDefault(d => d.ServiceType == typeof(ConsumerHostOptions))?.ImplementationInstance is ConsumerHostOptions existing)
            return existing;
        var options = new ConsumerHostOptions();
        services.AddSingleton(options);
        return options;
    }
}
