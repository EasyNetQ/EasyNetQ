using RabbitMQ.Client;

namespace EasyNetQ.DI
{
    public static class ConnectionFactoryFactory
    {
        public static IConnectionFactory CreateConnectionFactory(ConnectionConfiguration connectionConfiguration)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");

            var connectionFactory = new ConnectionFactory
            {
                UseBackgroundThreadsForIO = connectionConfiguration.UseBackgroundThreads,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = false
            };

            if (connectionConfiguration.AmqpConnectionString != null)
                connectionFactory.Uri = connectionConfiguration.AmqpConnectionString;

            if (connectionFactory.VirtualHost == "/")
                connectionFactory.VirtualHost = connectionConfiguration.VirtualHost;

            if (connectionFactory.UserName == "guest")
                connectionFactory.UserName = connectionConfiguration.UserName;

            if (connectionFactory.Password == "guest")
                connectionFactory.Password = connectionConfiguration.Password;

            if (connectionFactory.Port == -1)
                connectionFactory.Port = connectionConfiguration.Port;

            connectionFactory.RequestedHeartbeat = connectionConfiguration.RequestedHeartbeat;
            connectionFactory.ClientProperties = connectionConfiguration.ClientProperties;
            connectionFactory.AuthMechanisms = connectionConfiguration.AuthMechanisms;

            return connectionFactory;
        }
    }
}
