using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_nack_received_from_the_message_handler : ConsumerTestBase
{
    protected override void AdditionalSetUp()
    {
        StartConsumer((_, _, _, _) => AckStrategies.NackWithRequeue);
        DeliverMessage();
    }

    [Fact]
    public async Task Should_nack()
    {
        await MockBuilder.Channels[0].Received().BasicNackAsync(DeliverTag, false, true);
    }
}
