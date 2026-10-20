namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_nack_without_requeue_received_from_the_message_handler : ConsumerTestBase
{
    protected override async Task InitializeAsyncCore()
    {
#pragma warning disable IDISP004
        await StartConsumerAsync((_, _, _, _) => AckDecision.NackDiscard);
#pragma warning restore IDISP004
        await DeliverMessageAsync();
    }

    [Fact]
    public async Task Should_nack_without_requeue()
    {
        await MockBuilder.Channels[0].Received().BasicNackAsync(DeliverTag, false, false, cancellationToken: CancellationToken.None);
    }
}
