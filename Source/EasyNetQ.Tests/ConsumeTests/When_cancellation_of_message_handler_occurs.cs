using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_cancellation_of_message_handler_occurs : ConsumerTestBase
{
    protected override void AdditionalSetUp()
    {
        ConsumeErrorStrategy.HandleCancelledAsync(default)
            .ReturnsForAnyArgs(new ValueTask<AckStrategy>(AckStrategies.Ack));

        StartConsumer((_, _, _) =>
        {
            Cancellation.Cancel();
            Cancellation.Token.ThrowIfCancellationRequested();
            return AckStrategies.Ack;
        });
        DeliverMessage();
    }

    [Fact]
    public async Task Should_invoke_the_cancellation_strategy()
    {
        await ConsumeErrorStrategy.Received().HandleCancelledAsync(
            Arg.Is<ConsumeContext>(
                args => args.ReceivedInfo.ConsumerTag == ConsumerTag &&
                        args.ReceivedInfo.DeliveryTag == DeliverTag &&
                        args.ReceivedInfo.Exchange == "the_exchange" &&
                        args.Body.ToArray().SequenceEqual(OriginalBody)
            )
        );
    }

    [Fact]
    public void Should_ack()
    {
        MockBuilder.Channels[0].Received().BasicAck(DeliverTag, false);
    }
}
