using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_cancelled_by_the_user : IDisposable
{
    private readonly MockBuilder mockBuilder;

    public When_a_consumer_is_cancelled_by_the_user()
    {
        mockBuilder = new MockBuilder();

        var queue = new Queue("my_queue", false);

        using var cancelSubscription = mockBuilder.Bus.Advanced
            .Consume(queue, async (_, _, _) => await Task.Run(() => { }));

        using var are = new AutoResetEvent(false);
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((in ConsumerChannelDisposedEvent _) => are.Set());
#pragma warning restore IDISP004

        if (!are.WaitOne(5000))
        {
            throw new TimeoutException();
        }
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public void Should_dispose_the_model()
    {
        mockBuilder.Consumers[0].Channel.Received().Dispose();
    }
}
