using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class MockConnectionFactory : IConnectionFactory
    {
        public IConnection CreateConnection()
        {
            return new MockConnection();
        }

        public string HostName
        {
            get { return "localhost"; }
        }

        public string VirtualHost
        {
            get { return "/"; }
        }

        public string UserName
        {
            get { return "guest"; }
        }
    }
}