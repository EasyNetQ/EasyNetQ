using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_handled_is_received_from_the_message_handler : ConsumerTestBase
{
    protected override async Task InitializeAsyncCore()
    {
#pragma warning disable IDISP004
        await StartConsumerAsync((_, _, _, _) => AckDecision.Handled);
#pragma warning restore IDISP004
        await DeliverMessageAsync();
    }

    [Fact]
    public async Task Should_neither_ack_nor_nack()
    {
        await MockBuilder.Channels[0].DidNotReceive().BasicAckAsync(Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await MockBuilder.Channels[0].DidNotReceive().BasicNackAsync(Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
