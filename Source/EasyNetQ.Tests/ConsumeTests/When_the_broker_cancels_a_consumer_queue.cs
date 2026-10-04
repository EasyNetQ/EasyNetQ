using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_the_broker_cancels_a_consumer_queue : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder = new();
    private readonly TaskCompletionSource<ConsumerCancelledEvent> cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async ValueTask InitializeAsync()
    {
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((ConsumerCancelledEvent e) => Task.FromResult(cancelled.TrySetResult(e)));
        await mockBuilder.Bus.Advanced.ConsumeAsync(
#pragma warning restore IDISP004
            new Queue("my_queue", false),
            (_, _, _) => Task.CompletedTask,
            c => c.WithConsumerTag("consumer_tag")
        );

        await mockBuilder.Consumers[0].HandleBasicCancelAsync("consumer_tag");
    }

    public async ValueTask DisposeAsync() => await mockBuilder.DisposeAsync();

    [Fact]
    public async Task Should_publish_which_queue_was_cancelled()
    {
        var e = await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        e.Queue.Name.Should().Be("my_queue");
    }
}
