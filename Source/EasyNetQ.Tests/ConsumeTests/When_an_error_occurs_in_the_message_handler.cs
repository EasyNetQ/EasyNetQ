using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_an_error_occurs_in_the_message_handler : ConsumerTestBase
{
    private Exception exception;

    protected override void AdditionalSetUp()
    {
        exception = new Exception("I've had a bad day :(");

        ConsumeErrorStrategy.HandleErrorAsync(default, exception)
            .ReturnsForAnyArgs(new ValueTask<AckStrategy>(AckStrategies.Ack));

#pragma warning disable IDISP004
        StartConsumer((_, _, _, _) => throw exception);
#pragma warning restore IDISP004
        DeliverMessage();
    }

    [Fact]
    public async Task Should_invoke_the_error_strategy()
    {
        await ConsumeErrorStrategy.Received().HandleErrorAsync(
            Arg.Is<ConsumeContext>(args => args.ReceivedInfo.ConsumerTag == ConsumerTag &&
                                           args.ReceivedInfo.DeliveryTag == DeliverTag &&
                                           args.ReceivedInfo.Exchange == "the_exchange" &&
                                           args.Body.ToArray().SequenceEqual(OriginalBody)),
            Arg.Is<Exception>(e => e == exception)
        );
    }

    [Fact]
    public async Task Should_ack()
    {
        await MockBuilder.Channels[0].Received().BasicAckAsync(DeliverTag, false);
    }
}
