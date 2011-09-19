using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConnectionFactory
    {
        IConnection CreateConnection();
        string HostName { get; }
        string VirtualHost { get; }
        string UserName { get; }
    }

    public class ConnectionFactoryWrapper : IConnectionFactory
    {
        private readonly ConnectionFactory connectionFactory;

        public ConnectionFactoryWrapper(ConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public IConnection CreateConnection()
        {
            return connectionFactory.CreateConnection();
        }

        public string HostName
        {
            get { return connectionFactory.HostName; }
        }

        public string VirtualHost
        {
            get { return connectionFactory.VirtualHost; }
        }

        public string UserName
        {
            get { return connectionFactory.UserName; }
        }
    }
}