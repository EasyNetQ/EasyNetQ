namespace EasyNetQ.Tests.ConsumerTests;

public class When_disposed : Given_a_сonsumer
{
    public When_disposed()
    {
        consumer.StartConsumingAsync().GetAwaiter().GetResult(); ;
        consumer.Dispose();
    }

    [Fact]
    public void Should_dispose_the_internal_consumer()
    {
        internalConsumers[0].Received().Dispose();
    }
}
