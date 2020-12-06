using RabbitMQ.Client;

namespace EasyNetQ.DI
{
    internal static class ConnectionFactoryFactory
    {
        public static IConnectionFactory CreateConnectionFactory(ConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            var connectionFactory = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = false,
                VirtualHost = configuration.VirtualHost,
                UserName = configuration.UserName,
                Password = configuration.Password,
                Port = configuration.Port,
                RequestedHeartbeat = configuration.RequestedHeartbeat,
                ClientProperties = configuration.ClientProperties,
                AuthMechanisms = configuration.AuthMechanisms,
                ClientProvidedName = configuration.Name,
                NetworkRecoveryInterval = configuration.ConnectIntervalAttempt,
                ContinuationTimeout = configuration.Timeout
            };
            return connectionFactory;
        }
    }
}
