using EasyNetQ.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests;

/// <summary>
///     Builds context hierarchies for tests that exercise pipeline pieces directly
/// </summary>
internal static class TestContexts
{
    public static ConsumerContext Consumer(string queue = "queue", IServiceProvider? services = null)
        => new(new ChannelContext(new ConnectionContext("Consumer", services ?? new ServiceCollection().BuildServiceProvider())), queue);

    public static ConsumeContext Consume(
        in MessageReceivedInfo receivedInfo,
        in MessageProperties properties,
        ReadOnlyMemory<byte> body,
        IServiceProvider? services = null
    )
    {
        var context = new ConsumeContext(Consumer(receivedInfo.Queue, services))
        {
            ReceivedInfo = receivedInfo,
            Properties = properties,
            Body = body,
        };
        return context;
    }
}
