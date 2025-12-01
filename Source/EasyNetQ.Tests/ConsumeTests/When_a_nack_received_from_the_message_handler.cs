using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_nack_received_from_the_message_handler : ConsumerTestBase
{
    protected override async Task InitializeAsyncCore()
    {
#pragma warning disable IDISP004
        await StartConsumerAsync((_, _, _, _) => AckStrategies.NackWithRequeueAsync);
#pragma warning restore IDISP004
        await DeliverMessageAsync();
    }

    [Fact]
    public async Task Should_nack()
    {
        await MockBuilder.Channels[0].Received().BasicNackAsync(DeliverTag, false, true);
    }
}
