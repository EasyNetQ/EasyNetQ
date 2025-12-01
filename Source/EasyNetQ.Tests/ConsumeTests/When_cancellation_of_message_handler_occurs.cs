using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_cancellation_of_message_handler_occurs : ConsumerTestBase
{
    protected override void AdditionalSetUp()
    {
        ConsumeErrorStrategy.HandleCancelledAsync(default)
            .ReturnsForAnyArgs(new ValueTask<AckStrategyAsync>(AckStrategies.NackWithRequeueAsync));

        using var are = new AutoResetEvent(false);
        using var consumer = StartConsumer((_, _, _, ct) =>
        {
            are.Set();
            Task.Delay(-1, ct).GetAwaiter().GetResult();
            return AckStrategies.AckAsync;
        });
        var deliverTask = DeliverMessageAsync();
        are.WaitOne();
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
    public async Task Should_nack_with_requeue()
    {
        await MockBuilder.Channels[0].Received().BasicNackAsync(DeliverTag, false, true);
    }
}
