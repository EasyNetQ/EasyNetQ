namespace EasyNetQ.Tests.ConsumerTests;

public class When_disposed : Given_a_сonsumer
{
    protected override async Task InitializeAsyncCore()
    {
        await consumer.StartConsumingAsync();
        await consumer.DisposeAsync();
        await base.InitializeAsyncCore();
    }

    [Fact]
    public async Task Should_dispose_the_internal_consumer()
    {
        await internalConsumers[0].Received().DisposeAsync();
    }
}
