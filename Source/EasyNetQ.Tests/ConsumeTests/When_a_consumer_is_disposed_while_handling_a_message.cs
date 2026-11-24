using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_disposed_while_handling_a_message : ConsumerTestBase
{
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
        handlerStarted.Wait();

        var disposeTask = consumer.DisposeAsync().AsTask();
        releaseHandler.Set();

        await deliverTask;
        await disposeTask;
    }

    [Fact]
    public void Should_ack_the_message_before_disposing_the_model()
    {
        var channel = MockBuilder.Consumers[0].Channel;
        Received.InOrder(() =>
        {
            channel.BasicAckAsync(DeliverTag, false, CancellationToken.None);
            channel.DisposeAsync();
        });
    }
}
