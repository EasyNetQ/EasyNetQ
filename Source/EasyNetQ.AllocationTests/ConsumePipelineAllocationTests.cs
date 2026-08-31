using EasyNetQ.Benchmarks;
using EasyNetQ.Benchmarks.Fixtures;

namespace EasyNetQ.AllocationTests;

public class ConsumePipelineAllocationTests
{
    private static readonly ConsumePipelineFixture Fixture = new();

    [Fact]
    public void Consume_small_message()
    {
        var context = Fixture.CreateContext(SampleMessages.CreateSmall());
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.ConsumeDelegate(context));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.ConsumeSmall);
    }

    [Fact]
    public void Consume_medium_message()
    {
        var context = Fixture.CreateContext(SampleMessages.CreateMedium());
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.ConsumeDelegate(context));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.ConsumeMedium);
    }

    [Fact]
    public void Consume_large_message()
    {
        var context = Fixture.CreateContext(SampleMessages.CreateLarge());
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.ConsumeDelegate(context));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.ConsumeLarge);
    }
}
