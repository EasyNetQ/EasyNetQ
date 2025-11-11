using RabbitMQ.Client;

namespace EasyNetQ.DI;

internal static class ConnectionFactoryFactory
{
    public static IConnectionFactory CreateConnectionFactory(ConnectionConfiguration configuration)
    {
        var connectionFactory = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = false,
            VirtualHost = configuration.VirtualHost,
            UserName = configuration.UserName,
            Password = configuration.Password,
            Port = configuration.Port,
            RequestedHeartbeat = configuration.RequestedHeartbeat,
            ClientProperties = configuration.ClientProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value),
            AuthMechanisms = configuration.AuthMechanisms,
            ClientProvidedName = configuration.Name,
            NetworkRecoveryInterval = configuration.ConnectIntervalAttempt,
            ContinuationTimeout = configuration.Timeout,
            ConsumerDispatchConcurrency = configuration.ConsumerDispatcherConcurrency.HasValue ? (ushort)configuration.ConsumerDispatcherConcurrency.Value : configuration.PrefetchCount,
            RequestedChannelMax = configuration.RequestedChannelMax
        };

        if (configuration.Hosts.Count > 0)
            connectionFactory.HostName = configuration.Hosts[0].Host;

        return connectionFactory;
    }
}
