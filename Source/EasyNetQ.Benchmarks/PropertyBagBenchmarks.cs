using BenchmarkDotNet.Attributes;
using EasyNetQ.Benchmarks.Fixtures;
using EasyNetQ.Pipeline;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Typed property access on a bag with a few entries, and inherited lookups through the context hierarchy
/// </summary>
[MemoryDiagnoser]
public class PropertyBagBenchmarks
{
    private static readonly PropertyKey<string> First = new("first");
    private static readonly PropertyKey<int> Second = new("second");
    private static readonly PropertyKey<object> Third = new("third");
    private static readonly PropertyKey<string> Fourth = new("fourth");
    private static readonly PropertyKey<string> Missing = new("missing");

    private PropertyBag bag;
    private PipelineOverheadFixture fixture = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        bag = new PropertyBag();
        bag.Set(First, "a");
        bag.Set(Second, 2);
        bag.Set(Third, new object());
        bag.Set(Fourth, "d");
        fixture = new PipelineOverheadFixture();
    }

    [Benchmark]
    public bool TryGet_First() => bag.TryGet(First, out _);

    [Benchmark]
    public bool TryGet_Fourth() => bag.TryGet(Fourth, out _);

    [Benchmark]
    public bool TryGet_Missing() => bag.TryGet(Missing, out _);

    [Benchmark]
    public void Set_Existing_ReferenceType() => bag.Set(Fourth, "d");

    [Benchmark]
    public int Context_Get_Inherited_Three_Layers_Up() => fixture.ReadInheritedProperty();
}
