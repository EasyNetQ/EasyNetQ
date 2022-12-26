// ReSharper disable InconsistentNaming

namespace EasyNetQ.Tests.ConsumerTests;

public class When_disposed : Given_a_сonsumer
{
    public When_disposed()
    {
        consumer.StartConsuming();
        consumer.Dispose();
    }

    [Fact]
    public void Should_dispose_the_internal_consumer()
    {
        internalConsumers[0].Received().Dispose();
    }
}
