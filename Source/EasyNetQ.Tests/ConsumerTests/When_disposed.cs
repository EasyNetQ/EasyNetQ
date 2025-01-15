namespace EasyNetQ.Tests.ConsumerTests;

public class When_disposed : Given_a_сonsumer
{
    public When_disposed()
    {
        consumer.StartConsumingAsync().GetAwaiter().GetResult(); ;
        consumer.Dispose();
    }

    [Fact]
    public async Task Should_dispose_the_internal_consumer()
    {
        await internalConsumers[0].Received().DisposeAsync();
    }
}
