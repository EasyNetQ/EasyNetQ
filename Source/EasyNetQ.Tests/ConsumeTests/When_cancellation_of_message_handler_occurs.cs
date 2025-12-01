using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_cancellation_of_message_handler_occurs : ConsumerTestBase
{
    protected override async Task InitializeAsyncCore()
    {
        ConsumeErrorStrategy.HandleCancelledAsync(default)
            .ReturnsForAnyArgs(new ValueTask<AckStrategyAsync>(AckStrategies.NackWithRequeueAsync));

        using var are = new AutoResetEvent(false);
        Task deliverTask;
        await using (await StartConsumerAsync((_, _, _, ct) =>
        {
            are.Set();
            Task.Delay(-1, ct).GetAwaiter().GetResult();
            return AckStrategies.AckAsync;
        }))
        {
            deliverTask = DeliverMessageAsync();
            are.WaitOne();
        }
        await deliverTask;
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
