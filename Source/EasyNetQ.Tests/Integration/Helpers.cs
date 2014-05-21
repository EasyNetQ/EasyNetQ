using EasyNetQ.Management.Client;

namespace EasyNetQ.Tests.Integration
{
    public static class Helpers
    {
        public static IManagementClient GetClient()
        {
            return new ManagementClient("http://localhost", "guest", "guest", 15672);
        }

        public static void CloseConnection()
        {
            var client = GetClient();
            foreach (var clientConnection in client.GetConnections())
            {
                client.CloseConnection(clientConnection);
            }
        }

        public static void ClearAllQueues()
        {
            var client = GetClient();
            foreach (var queue in client.GetQueues())
            {
                client.Purge(queue);
            }
        }
    }
}