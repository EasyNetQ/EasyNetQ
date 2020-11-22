using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Management.Client;

namespace EasyNetQ.IntegrationTests.Utils
{
    public static class ManagementClientExtensions
    {
        public static async Task KillAllConnectionsAsync(
            this IManagementClient client, CancellationToken cancellationToken
        )
        {
            // We need this crunch with loop because it seems that management plugin has some delay in showing info
            // and we could receive an empty list of connections even if connections are open
            var wasClosed = false;
            do
            {
                var connections = await client.GetConnectionsAsync(cancellationToken);
                foreach (var connection in connections)
                {
                    await client.CloseConnectionAsync(connection, cancellationToken);
                    wasClosed = true;
                }

                await Task.Delay(500, cancellationToken);
            } while (!wasClosed);
        }
    }
}
