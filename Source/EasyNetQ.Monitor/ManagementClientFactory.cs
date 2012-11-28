using EasyNetQ.Management.Client;

namespace EasyNetQ.Monitor
{
    public interface IManagementClientFactory
    {
        IManagementClient CreateManagementClient(Broker broker);
    }

    public class ManagementClientFactory : IManagementClientFactory
    {
        public IManagementClient CreateManagementClient(Broker broker)
        {
            return new ManagementClient(broker.ManagementUrl, broker.Username, broker.Password);
        }
    }
}