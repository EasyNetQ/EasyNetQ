using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class MockConnectionFactory : IConnectionFactory
    {
        public IConnection Connection { get; set; }

        public MockConnectionFactory(IConnection connection)
        {
            Connection = connection;
        }

        public IConnection CreateConnection()
        {
            return Connection;
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