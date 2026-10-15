using EasyNetQ.Benchmarks;
using EasyNetQ.Benchmarks.Fixtures;

namespace EasyNetQ.AllocationTests;

public class ConsumePipelineAllocationTests
{
    private static readonly ConsumePipelineFixture Fixture = new();

    [Fact]
    public void Consume_small_message()
    {
        var (properties, body) = Fixture.Serialize(SampleMessages.CreateSmall());
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.ConsumeAsync(properties, body));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.ConsumeSmall);
    }

    [Fact]
    public void Consume_medium_message()
    {
        var (properties, body) = Fixture.Serialize(SampleMessages.CreateMedium());
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.ConsumeAsync(properties, body));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.ConsumeMedium);
    }

    [Fact]
    public void Consume_large_message()
    {
        var (properties, body) = Fixture.Serialize(SampleMessages.CreateLarge());
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.ConsumeAsync(properties, body));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.ConsumeLarge);
    }
}
