using EasyNetQ.Persistent;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IAdvancedBus"/>
/// </summary>
public static partial class AdvancedBusExtensions
{
    /// <summary>
    /// Initialises all connections, but does not check if they are connected currently.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The stats of the queue</returns>
    [Obsolete("ConnectAsync is deprecated because it is misleading. Please use EnsureConnectedAsync instead")]
    public static async Task ConnectAsync(
        this IAdvancedBus bus, CancellationToken cancellationToken = default
    )
    {
        foreach (PersistentConnectionType type in Enum.GetValues(typeof(PersistentConnectionType)))
        {
            try
            {
                await bus.EnsureConnectedAsync(type, cancellationToken).ConfigureAwait(false);
            }
            catch (AlreadyClosedException)
            {
            }
        }
    }
}
