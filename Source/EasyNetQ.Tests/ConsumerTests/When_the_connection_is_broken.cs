using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumerTests;

public class When_the_connection_is_broken : Given_a_сonsumer
{
    public When_the_connection_is_broken()
    {
        consumer.StartConsuming();
        eventBus.Publish(new ConnectionRecoveredEvent(PersistentConnectionType.Consumer, new AmqpTcpEndpoint()));
    }

    [Fact]
    public void Should_re_create_internal_consumer()
    {
        internalConsumerFactory.Received(1).CreateConsumer(Arg.Any<ConsumerConfiguration>());
        internalConsumers.Count.Should().Be(1);
        internalConsumers[0].Received(2).StartConsuming(Arg.Any<bool>());
    }
}
