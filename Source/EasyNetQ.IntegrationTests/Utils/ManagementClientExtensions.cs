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
            var connections = await client.GetConnectionsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var connection in connections)
                await client.CloseConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }
}
