using EasyNetQ.ConnectionString;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyNetQ;

/// <summary>
/// Static methods to create EasyNetQ core APIs.
/// </summary>
public static class RabbitHutch
{
    /// <summary>
    /// Registers a new instance of <see cref="SelfHostedBus"/>.
    /// </summary>
    /// <param name="services">
    /// the service collection to register the bus in.
    /// </param>
    /// <param name="connectionString">
    /// The EasyNetQ connection string. Example:
    /// host=192.168.1.1;port=5672;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword;requestedHeartbeat=10
    ///
    /// The following default values will be used if not specified:
    /// host=localhost;port=5672;virtualHost=/;username=guest;password=guest;requestedHeartbeat=10
    /// </param>
    public static IServiceCollection AddEasyNetQ(this IServiceCollection services, string connectionString)
    {
        services.TryAddSingleton<ConnectionConfiguration>( sp => sp.GetRequiredService<IConnectionStringParser>().Parse(connectionString));
        return services.AddEasyNetQ();
    }

    /// <summary>
    /// Registers a new instance of <see cref="SelfHostedBus"/> using all default dependencies.
    /// </summary>
    public static IServiceCollection AddEasyNetQ(this IServiceCollection services)
    {
        return services.RegisterDefaultServices();
    }


    /// <summary>
    /// Registers a new instance of <see cref="SelfHostedBus"/> using the specified connection configuration in the service collection.
    /// </summary>
    /// <param name="configurator">
    /// </param>
    /// <param name="services">
    /// </param>
    public static IServiceCollection AddEasyNetQ(this IServiceCollection services, Action<ConnectionConfiguration> configurator)
    {
        var connectionConfiguration = new ConnectionConfiguration();
        connectionConfiguration.SetDefaultProperties();

        configurator(connectionConfiguration);

        services.TryAddSingleton(resolver => connectionConfiguration);
        services.RegisterDefaultServices();

        return services;
    }
}
