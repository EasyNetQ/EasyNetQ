using System.Collections.Generic;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class MockConnectionFactory : ConnectionFactoryWrapper
    {
        public IConnection Connection { get; set; }

        public MockConnectionFactory(IConnection connection) : base(new ConnectionConfiguration
        {
            Hosts = new List<IHostConfiguration>
            {
                new HostConfiguration()
            }
        })
        {
            Connection = connection;
        }

        public override IConnection CreateConnection()
        {
            return Connection;
        }
    }
}