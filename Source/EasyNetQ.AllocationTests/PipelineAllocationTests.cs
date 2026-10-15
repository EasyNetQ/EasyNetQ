using EasyNetQ.Benchmarks.Fixtures;
using EasyNetQ.Pipeline;

namespace EasyNetQ.AllocationTests;

public class PipelineAllocationTests
{
    private static readonly PropertyKey<string> Key = new("key");
    private static readonly PipelineOverheadFixture Fixture = new();

    [Fact]
    public void Property_bag_get_and_set_steady_state()
    {
        var bag = new PropertyBag();
        bag.Set(Key, "value");
        var bytes = AllocationAssert.BytesPerIteration(() =>
        {
            bag.Set(Key, "other");
            bag.TryGet(Key, out _);
        });
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.PropertyBagGetSet);
    }

    [Fact]
    public void Inherited_property_lookup_through_pooled_context()
    {
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.ReadInheritedProperty());
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.ContextInheritedGet);
    }

    [Fact]
    public void Consume_pipeline_plumbing_with_noop_terminal()
    {
        var bytes = AllocationAssert.BytesPerIteration(() => Fixture.ConsumeNoopAsync());
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.PipelineOverheadNoop);
    }
}
