using System.Collections.Generic;
using RabbitMQ.Client;

namespace EasyNetQ.InMemoryClient
{
    public class InMemoryConnectionFactory : ConnectionFactoryWrapper
    {
        public InMemoryConnection CurrentConnection { get; private set; }

        public InMemoryConnectionFactory() : base(new ConnectionConfiguration
        {
            Hosts = new List<IHostConfiguration> { new HostConfiguration() }
        }, new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>())
        {
        }

        public override IConnection CreateConnection()
        {
            return (CurrentConnection = new InMemoryConnection());
        }
    }
}