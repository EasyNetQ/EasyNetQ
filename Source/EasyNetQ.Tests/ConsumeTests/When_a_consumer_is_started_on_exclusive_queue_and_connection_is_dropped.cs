using EasyNetQ.Events;
using EasyNetQ.Persistent;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_started_on_exclusive_queue_and_connection_is_dropped : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;

    public When_a_consumer_is_started_on_exclusive_queue_and_connection_is_dropped()
    {
        mockBuilder = new MockBuilder();
    }

    public async Task InitializeAsync()
    {
        var queue = new Queue("my_queue", false, true);
        await using var cancelSubscription = await mockBuilder.Bus.Advanced
            .ConsumeAsync(queue, async (_, _, _) => await Task.Run(() => { }));

        using var stopped = new AutoResetEvent(false);
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((StoppedConsumingEvent _) => Task.FromResult(stopped.Set()));
#pragma warning restore IDISP004

        await mockBuilder.EventBus.PublishAsync(new ConnectionDisconnectedEvent(PersistentConnectionType.Consumer, Substitute.For<AmqpTcpEndpoint>(), "Unknown"));
        await mockBuilder.EventBus.PublishAsync(new ConnectionRecoveredEvent(PersistentConnectionType.Consumer, Substitute.For<AmqpTcpEndpoint>()));

        if (!stopped.WaitOne(5000))
        {
            throw new TimeoutException();
        }
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_dispose_the_model()
    {
        mockBuilder.Consumers[0].Channel.Received().DisposeAsync();
    }
}
