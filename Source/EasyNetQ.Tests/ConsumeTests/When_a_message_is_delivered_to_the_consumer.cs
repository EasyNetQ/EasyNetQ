using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_message_is_delivered_to_the_consumer : ConsumerTestBase
{
    protected override async Task InitializeAsyncCore()
    {
#pragma warning disable IDISP004
        await StartConsumerAsync((_, _, _, _) => AckStrategies.AckAsync);
#pragma warning restore IDISP004
        await DeliverMessageAsync();
    }

    [Fact]
    public void Should_invoke_consumer()
    {
        ConsumerWasInvoked.Should().BeTrue();
    }

    [Fact]
    public void Should_deliver_the_message_body()
    {
        DeliveredMessageBody.ToArray().Should().BeEquivalentTo(OriginalBody);
    }

    [Fact]
    public void Should_deliver_the_message_properties()
    {
        DeliveredMessageProperties.Type.Should().BeSameAs(OriginalProperties.Type);
    }

    [Fact]
    public void Should_deliver_the_consumer_tag()
    {
        DeliveredMessageInfo.ConsumerTag.Should().Be(ConsumerTag);
    }

    [Fact]
    public void Should_deliver_the_delivery_tag()
    {
        DeliveredMessageInfo.DeliveryTag.Should().Be(DeliverTag);
    }

    [Fact]
    public void Should_deliver_the_exchange_name()
    {
        DeliveredMessageInfo.Exchange.Should().Be("the_exchange");
    }

    [Fact]
    public void Should_deliver_the_routing_key()
    {
        DeliveredMessageInfo.RoutingKey.Should().Be("the_routing_key");
    }

    [Fact]
    public void Should_deliver_redelivered_flag()
    {
        DeliveredMessageInfo.Redelivered.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ack_the_message()
    {
        await MockBuilder.Channels[0].Received().BasicAckAsync(DeliverTag, false);
    }
}
