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
            ConsumerDispatchConcurrency = configuration.ConsumerDispatcherConcurrency ?? 1,
            RequestedChannelMax = configuration.RequestedChannelMax
        };

        if (configuration.MaxInboundMessageBodySize.HasValue)
            connectionFactory.MaxInboundMessageBodySize = configuration.MaxInboundMessageBodySize.Value;
        if (configuration.SocketReadTimeout.HasValue)
            connectionFactory.SocketReadTimeout = configuration.SocketReadTimeout.Value;
        if (configuration.SocketWriteTimeout.HasValue)
            connectionFactory.SocketWriteTimeout = configuration.SocketWriteTimeout.Value;
        if (configuration.RequestedConnectionTimeout.HasValue)
            connectionFactory.RequestedConnectionTimeout = configuration.RequestedConnectionTimeout.Value;
        if (configuration.HandshakeContinuationTimeout.HasValue)
            connectionFactory.HandshakeContinuationTimeout = configuration.HandshakeContinuationTimeout.Value;
        if (configuration.EndpointResolverFactory != null)
            connectionFactory.EndpointResolverFactory = configuration.EndpointResolverFactory;
        if (configuration.CredentialsProvider != null)
            connectionFactory.CredentialsProvider = configuration.CredentialsProvider;

        if (configuration.Hosts.Count > 0)
            connectionFactory.HostName = configuration.Hosts[0].Host;

        return connectionFactory;
    }
}
