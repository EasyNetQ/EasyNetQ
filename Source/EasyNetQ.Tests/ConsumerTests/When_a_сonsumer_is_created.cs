using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumerTests;

public class WhenAСonsumerStartsConsuming : Given_a_сonsumer
{
    public WhenAСonsumerStartsConsuming()
    {
        consumer.StartConsuming();
    }

    [Fact]
    public void Should_ask_the_internal_consumer_to_start_consuming()
    {
        internalConsumers[0].Received().StartConsuming();
    }

    [Fact]
    public void Should_create_internal_consumer()
    {
        internalConsumerFactory.Received(1).CreateConsumer(Arg.Any<ConsumerConfiguration>());
    }
}
