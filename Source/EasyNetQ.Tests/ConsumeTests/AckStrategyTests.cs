using EasyNetQ.Consumer;
using EasyNetQ.Events;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public class Ack_strategy
{
    public Ack_strategy()
    {
        channel = Substitute.For<IChannel, IRecoverable>();

        result = AckStrategies.AckAsync(channel, deliveryTag, CancellationToken.None).GetAwaiter().GetResult();
    }

    private readonly IChannel channel;
    private readonly AckResult result;
    private const ulong deliveryTag = 1234;

    [Fact]
    public async Task Should_ack_message()
    {
        await channel.Received().BasicAckAsync(deliveryTag, false);
    }

    [Fact]
    public void Should_return_Ack()
    {
        Assert.Equal(AckResult.Ack, result);
    }
}

public class NackWithoutRequeue_strategy
{
    public NackWithoutRequeue_strategy()
    {
        channel = Substitute.For<IChannel, IRecoverable>();

        result = AckStrategies.NackWithoutRequeueAsync(channel, deliveryTag, CancellationToken.None).GetAwaiter().GetResult();
    }

    private readonly IChannel channel;
    private readonly AckResult result;
    private const ulong deliveryTag = 1234;

    [Fact]
    public async Task Should_nack_message_and_not_requeue()
    {
        await channel.Received().BasicNackAsync(deliveryTag, false, false);
    }

    [Fact]
    public void Should_return_Nack()
    {
        Assert.Equal(AckResult.Nack, result);
    }
}

public class NackWithRequeue_trategy
{
    private readonly IChannel channel;
    private const ulong deliveryTag = 1234;

    public NackWithRequeue_trategy()
    {
        channel = Substitute.For<IChannel, IRecoverable>();
    }

    [Fact]
    public async Task Should_nack_message_and_requeue()
    {
        var result = await AckStrategies.NackWithRequeueAsync(channel, deliveryTag, CancellationToken.None);

        await channel.Received().BasicNackAsync(deliveryTag, false, true);
        Assert.Equal(AckResult.Nack, result);
    }
}
