using System.Diagnostics;
using EasyNetQ.Events;
using EasyNetQ.Persistent;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_cancelled_via_channel_shutdown : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;
    private readonly ManualResetEventSlim stopped = new(false);

    public When_a_consumer_is_cancelled_via_channel_shutdown()
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

#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((StoppedConsumingEvent _) => { stopped.Set(); return Task.CompletedTask; });
#pragma warning restore IDISP004

        // A lost connection shuts the channel down, which cancels every consumer on it. That is
        // not a broker-side cancellation, so the subscription must survive and come back.
        var closeReason = new ShutdownEventArgs(ShutdownInitiator.Peer, AmqpErrorCodes.ConnectionClosed, "Connection closed");
        mockBuilder.Consumers[0].Channel.CloseReason.Returns(closeReason);
        await mockBuilder.Consumers[0].HandleChannelShutdownAsync(mockBuilder.Consumers[0].Channel, closeReason);

        // The cancellation is handled on a detached task, so give it a moment to unregister the
        // consumer before the restart is triggered.
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        await mockBuilder.EventBus.PublishAsync(
            new ConnectionRecoveredEvent(PersistentConnectionType.Consumer, Substitute.For<AmqpTcpEndpoint>())
        );

        // Either the recovery event or the periodic restart may declare the consumer again.
        await WaitUntilAsync(() => mockBuilder.Consumers.Count > 1, TimeSpan.FromSeconds(10));
    }

    public async ValueTask DisposeAsync()
    {
        stopped.Dispose();
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_not_stop_consuming()
    {
        stopped.IsSet.Should().BeFalse();
    }

    [Fact]
    public void Should_declare_the_consumer_again()
    {
        mockBuilder.Consumers.Count.Should().Be(2);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout && !condition())
            await Task.Delay(TimeSpan.FromMilliseconds(20));
    }
}
