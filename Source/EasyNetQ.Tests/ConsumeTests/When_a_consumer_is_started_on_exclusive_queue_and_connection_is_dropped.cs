// ReSharper disable InconsistentNaming

using EasyNetQ.Events;
using EasyNetQ.Persistent;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_started_on_exclusive_queue_and_connection_is_dropped : IDisposable
{
    private readonly MockBuilder mockBuilder;

    public When_a_consumer_is_started_on_exclusive_queue_and_connection_is_dropped()
    {
        mockBuilder = new MockBuilder();

        var queue = new Queue("my_queue", false, true);
        using var cancelSubscription = mockBuilder.Bus.Advanced
            .Consume(queue, (_, _, _) => Task.Run(() => { }));

        var stopped = new AutoResetEvent(false);
        mockBuilder.EventBus.Subscribe((in StoppedConsumingEvent _) => stopped.Set());

        mockBuilder.EventBus.Publish(new ConnectionDisconnectedEvent(PersistentConnectionType.Consumer, Substitute.For<AmqpTcpEndpoint>(), "Unknown"));
        mockBuilder.EventBus.Publish(new ConnectionRecoveredEvent(PersistentConnectionType.Consumer, Substitute.For<AmqpTcpEndpoint>()));

        if (!stopped.WaitOne(5000))
        {
            throw new TimeoutException();
        }
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public void Should_dispose_the_model()
    {
        mockBuilder.Consumers[0].Model.Received().Dispose();
    }
}

// ReSharper restore InconsistentNaming
