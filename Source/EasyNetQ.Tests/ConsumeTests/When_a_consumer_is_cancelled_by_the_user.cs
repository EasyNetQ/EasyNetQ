using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_cancelled_by_the_user : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;

#pragma warning disable IDISP017
    public When_a_consumer_is_cancelled_by_the_user()
    {
        mockBuilder = new MockBuilder();
    }
#pragma warning restore IDISP004
    public async ValueTask InitializeAsync()
    {
        var queue = new Queue("my_queue", false);

        var cancelSubscription = await mockBuilder.Bus.Advanced
            .ConsumeAsync(queue, async (_, _, _) => await Task.Run(() => { }));

        using var are = new AutoResetEvent(false);

        using var _ = mockBuilder.EventBus.Subscribe((ConsumerChannelDisposedEvent _) => Task.FromResult(are.Set()));

        await cancelSubscription.DisposeAsync();

        if (!are.WaitOne(5000))
        {
            throw new TimeoutException();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_dispose_the_model()
    {
        mockBuilder.Consumers[0].Channel.Received().DisposeAsync();
    }

    [Fact]
    public void Should_not_cancel_the_consumer_before_disposing_the_model()
    {
        mockBuilder.Consumers[0].Channel.DidNotReceive().BasicCancelAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
