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
    /// Registers a new instance of <see cref="RabbitBus"/>.
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
        return services.RegisterDefaultServices(
            s =>
            {
                var connectionStringParser = s.GetRequiredService<IConnectionStringParser>();
                return connectionStringParser.Parse(connectionString);
            }
        );
    }

    /// <summary>
    /// Registers a new instance of <see cref="RabbitBus"/> using all default dependencies.
    /// </summary>
    public static IServiceCollection AddEasyNetQ(this IServiceCollection services)
    {
        return services.RegisterDefaultServices(_ => new ConnectionConfiguration());
    }

    /// <summary>
    /// Registers a new instance of <see cref="RabbitBus"/> using the specified connection configuration in the service collection.
    /// </summary>
    /// <param name="configurator">
    /// </param>
    /// <param name="services">
    /// </param>
    public static IServiceCollection AddEasyNetQ(this IServiceCollection services, Action<ConnectionConfiguration> configurator)
    {
        return services.RegisterDefaultServices(
            _ =>
            {
                var configuration = new ConnectionConfiguration();
                configurator(configuration);
                return configuration;
            }
        );
    }
}
