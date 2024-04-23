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
    public void Should_nack()
    {
        MockBuilder.Channels[0].Received().BasicNack(DeliverTag, false, true);
    }
}
