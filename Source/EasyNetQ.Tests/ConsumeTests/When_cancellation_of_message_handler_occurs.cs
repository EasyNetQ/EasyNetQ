using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_cancellation_of_message_handler_occurs : ConsumerTestBase
{
    protected override void AdditionalSetUp()
    {
        ConsumeErrorStrategy.HandleCancelledAsync(default)
            .ReturnsForAnyArgs(new ValueTask<AckStrategy>(AckStrategies.NackWithRequeue));

        var are = new AutoResetEvent(false);
        var consumer = StartConsumer((_, _, _, ct) =>
        {
            are.Set();
            Task.Delay(-1, ct).GetAwaiter().GetResult();
            return AckStrategies.Ack;
        });
        var deliverTask = DeliverMessageAsync();
        are.WaitOne();
        consumer.Dispose();
        deliverTask.GetAwaiter().GetResult();
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
    public void Should_nack_with_requeue()
    {
        MockBuilder.Channels[0].Received().BasicNack(DeliverTag, false, true);
    }
}
