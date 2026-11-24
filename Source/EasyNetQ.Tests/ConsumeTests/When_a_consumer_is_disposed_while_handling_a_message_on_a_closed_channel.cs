using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_disposed_while_handling_a_message_on_a_closed_channel : ConsumerTestBase
{
    private bool disposedWhileHandling;

    protected override async Task InitializeAsyncCore()
    {
        using var handlerStarted = new ManualResetEventSlim(false);
        using var releaseHandler = new ManualResetEventSlim(false);

        var consumer = await StartConsumerAsync((_, _, _, _) =>
        {
            handlerStarted.Set();
            // Ignores the cancellation token, so it only completes once released
            releaseHandler.Wait();
            return AckDecision.Ack;
        });

        var deliverTask = DeliverMessageAsync();
        handlerStarted.Wait(TestContext.Current.CancellationToken);

        // The broker has requeued the message already, so there is nothing to wait for
        MockBuilder.Consumers[0].Channel.IsOpen.Returns(false);

        var disposeTask = consumer.DisposeAsync().AsTask();
        var completedTask = await Task.WhenAny(
            disposeTask, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)
        );
        disposedWhileHandling = completedTask == disposeTask;
        releaseHandler.Set();

        await deliverTask;
        await disposeTask;
    }

    [Fact]
    public void Should_not_wait_for_the_handler()
    {
        disposedWhileHandling.Should().BeTrue();
    }
}
