using EasyNetQ.Benchmarks;
using EasyNetQ.Benchmarks.Fixtures;

namespace EasyNetQ.AllocationTests;

public class PublishPipelineAllocationTests
{
    private static readonly PublishPipelineFixture Fixture = new();

    [Fact]
    public void Advanced_publish_small_message()
    {
        var message = SampleMessages.CreateSmall();
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.PublishAdvanced(message));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.PublishAdvancedSmall);
    }

    [Fact]
    public void PubSub_publish_small_message()
    {
        var message = SampleMessages.CreateSmall();
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.PublishPubSub(message));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.PublishPubSubSmall);
    }
}
