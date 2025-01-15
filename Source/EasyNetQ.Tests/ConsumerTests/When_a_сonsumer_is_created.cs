using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumerTests;

public class WhenAСonsumerStartsConsuming : Given_a_сonsumer
{
    public WhenAСonsumerStartsConsuming()
    {
        consumer.StartConsumingAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void Should_ask_the_internal_consumer_to_start_consuming()
    {
        internalConsumers[0].Received().StartConsumingAsync().GetAwaiter();
    }

    [Fact]
    public void Should_create_internal_consumer()
    {
#pragma warning disable IDISP004
        internalConsumerFactory.Received(1).CreateConsumer(Arg.Any<ConsumerConfiguration>());
#pragma warning restore IDISP004
    }
}
