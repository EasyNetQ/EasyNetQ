using EasyNetQ.ConnectionString;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

/// <summary>
/// Static methods to create EasyNetQ core APIs.
/// </summary>
public static class RabbitHutch
{
    /// <summary>
    /// Registers a new instance of <see cref="RabbitBus"/> (the same as AddEasyNetQ for backward compatibility)/>.
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
    public static IEasyNetQBuilder AddEasyNetQ(this IServiceCollection services, string connectionString)
    {
        services.RegisterDefaultServices(
            s =>
            {
                var connectionStringParser = s.GetRequiredService<IConnectionStringParser>();
                return connectionStringParser.Parse(connectionString);
            }
        );
        return new EasyNetQBuilder(services);
    }

    /// <summary>
    /// Registers a new instance of <see cref="RabbitBus"/> using all default dependencies.
    /// </summary>
    public static IEasyNetQBuilder AddEasyNetQ(this IServiceCollection services)
    {
        services.RegisterDefaultServices(_ => new ConnectionConfiguration());
        return new EasyNetQBuilder(services);
    }

    /// <summary>
    /// Registers a new instance of <see cref="RabbitBus"/> using the specified connection configuration in the service collection.
    /// </summary>
    /// <param name="configurator">
    /// </param>
    /// <param name="services">
    /// </param>
    public static IEasyNetQBuilder AddEasyNetQ(this IServiceCollection services, Action<ConnectionConfiguration> configurator)
    {
        services.RegisterDefaultServices(
            _ =>
            {
                var configuration = new ConnectionConfiguration();
                configurator(configuration);
                return configuration;
            }
        );
        return new EasyNetQBuilder(services);
    }
    /// <summary>
    /// Registers a new instance of <see cref="RabbitBus"/> using the specified connection configuration in the service collection.
    /// </summary>
    /// <param name="connectionConfigurationFactory">
    /// </param>
    /// <param name="services">
    /// </param>
    public static IEasyNetQBuilder AddEasyNetQ(this IServiceCollection services, Func<IServiceProvider, ConnectionConfiguration> connectionConfigurationFactory)
    {
        services.RegisterDefaultServices(connectionConfigurationFactory);
        return new EasyNetQBuilder(services);
    }
}
