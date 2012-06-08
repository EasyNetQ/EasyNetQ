using RabbitMQ.Client;

namespace EasyNetQ.InMemoryClient
{
    public class InMemoryConnectionFactory : IConnectionFactory
    {
        public IConnection CreateConnection()
        {
            return new InMemoryConnection();
        }

        public string HostName
        {
            get { return "TheHostName"; }
        }

        public string VirtualHost
        {
            get { return "TheVirtualHostName"; }
        }

        public string UserName
        {
            get { return "TheUserName"; }
        }
    }
}