using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_an_error_occurs_in_the_message_handler : ConsumerTestBase
{
    private Exception exception;

    protected override void AdditionalSetUp()
    {
        ConsumerErrorStrategy.HandleConsumerErrorAsync(default, null)
            .ReturnsForAnyArgs(Task.FromResult(AckStrategies.Ack));

        exception = new Exception("I've had a bad day :(");
        StartConsumer((_, _, _) => throw exception);
        DeliverMessage();
    }

    [Fact]
    public async Task Should_invoke_the_error_strategy()
    {
        await ConsumerErrorStrategy.Received().HandleConsumerErrorAsync(
            Arg.Is<ConsumerExecutionContext>(args => args.ReceivedInfo.ConsumerTag == ConsumerTag &&
                                                     args.ReceivedInfo.DeliveryTag == DeliverTag &&
                                                     args.ReceivedInfo.Exchange == "the_exchange" &&
                                                     args.Body.ToArray().SequenceEqual(OriginalBody)),
            Arg.Is<Exception>(e => e == exception),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void Should_ack()
    {
        MockBuilder.Channels[0].Received().BasicAck(DeliverTag, false);
    }
}
