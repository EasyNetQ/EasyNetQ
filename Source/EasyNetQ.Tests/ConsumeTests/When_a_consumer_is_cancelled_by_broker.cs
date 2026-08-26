using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_cancelled_by_broker : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;
    private readonly ManualResetEventSlim stopped = new(false);

    public When_a_consumer_is_cancelled_by_broker()
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

        // The broker cancels the consumer on its own, for instance because the queue was deleted.
        // The channel stays open, so there is nothing to come back to and the subscription ends.
        await mockBuilder.Consumers[0].HandleBasicCancelAsync("consumer_tag");

        stopped.Wait(TimeSpan.FromSeconds(10));
    }

    public async ValueTask DisposeAsync()
    {
        stopped.Dispose();
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_stop_consuming()
    {
        stopped.IsSet.Should().BeTrue();
    }
}
