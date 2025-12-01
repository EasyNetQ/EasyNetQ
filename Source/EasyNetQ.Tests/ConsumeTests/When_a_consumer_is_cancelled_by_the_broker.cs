using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_cancelled_by_the_broker : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;

    public When_a_consumer_is_cancelled_by_the_broker()
    {
        mockBuilder = new MockBuilder();
    }

    public async Task InitializeAsync()
    {
        var queue = new Queue("my_queue", false);

#pragma warning disable IDISP004
        await mockBuilder.Bus.Advanced.ConsumeAsync(
#pragma warning restore IDISP004
            queue,
            (_, _, _) => Task.Run(() => { }),
            c => c.WithConsumerTag("consumer_tag")
        );
        using var are = new AutoResetEvent(false);
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((ConsumerChannelDisposedEvent _) => Task.FromResult(are.Set()));
#pragma warning restore IDISP004

        await mockBuilder.Consumers[0].HandleBasicCancelAsync("consumer_tag");

        if (!are.WaitOne(5000))
        {
            throw new TimeoutException();
        }
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_dispose_of_the_model()
    {
        mockBuilder.Consumers[0].Channel.Received().DisposeAsync();
    }
}
