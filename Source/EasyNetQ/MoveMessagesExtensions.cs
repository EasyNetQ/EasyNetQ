using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ;

public static class MoveMessagesExtensions
{
    public static async Task MoveMessagesAsync(this IAdvancedBus bus, Queue source, Queue destination, CancellationToken cancellationToken)
    {
        using var pullingConsumer = bus.CreatePullingConsumer(source, false);
        while (true)
        {
            using var pullResult = await pullingConsumer.PullAsync(cancellationToken).ConfigureAwait(false);

            if (!pullResult.IsAvailable) return;

            await bus.PublishAsync(
                Exchange.Default, destination.Name, true, pullResult.Properties, pullResult.Body, cancellationToken
            ).ConfigureAwait(false);

            await pullingConsumer.AckAsync(pullResult.ReceivedInfo.DeliveryTag, cancellationToken).ConfigureAwait(false);
        }
    }
}
