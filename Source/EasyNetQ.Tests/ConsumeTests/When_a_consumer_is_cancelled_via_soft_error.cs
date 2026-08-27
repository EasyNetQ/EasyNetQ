using EasyNetQ.Persistent;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_cancelled_via_soft_error : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;

    public When_a_consumer_is_cancelled_via_soft_error()
    {
        mockBuilder = new MockBuilder();
    }

    public async ValueTask InitializeAsync()
    {
        var queue = new Queue("my_queue", false);

#pragma warning disable IDISP004
        await mockBuilder.Bus.Advanced.ConsumeAsync(
#pragma warning restore IDISP004
            queue,
            (_, _, _) => Task.Run(() => { }),
            c => c.WithConsumerTag("consumer_tag")
        );

        var closeReason = new ShutdownEventArgs(ShutdownInitiator.Peer, AmqpErrorCodes.PreconditionFailed, "Oops");
        mockBuilder.Consumers[0].Channel.CloseReason.Returns(closeReason);
        // A channel-level error closes the channel, so the broker never sends Basic.Cancel:
        // the consumer learns about it through the channel shutdown notification.
        await mockBuilder.Consumers[0].HandleChannelShutdownAsync(mockBuilder.Consumers[0].Channel, closeReason);
        // Wait for a periodic consumer restart
        await Task.Delay(TimeSpan.FromSeconds(10));
    }

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_recreate_model_and_consumer()
    {
        mockBuilder.Consumers[0].Channel.Received().DisposeAsync();
        mockBuilder.Consumers[1].Channel.DidNotReceive().Dispose();
    }
}
