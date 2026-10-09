using BenchmarkDotNet.Attributes;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Wire type-name round trips on the cached (steady-state) path, as they happen once per published
///     and once per consumed message.
/// </summary>
[MemoryDiagnoser]
public class TypeNameBenchmarks
{
    private readonly DefaultTypeNameSerializer defaultSerializer = new();
    private readonly LegacyTypeNameSerializer legacySerializer = new();

    private string defaultName = null!;
    private string defaultGenericName = null!;
    private string legacyName = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        defaultName = defaultSerializer.Serialize(typeof(SmallMessage));
        defaultGenericName = defaultSerializer.Serialize(typeof(List<SmallMessage>));
        legacyName = legacySerializer.Serialize(typeof(SmallMessage));

        // populate caches
        defaultSerializer.Deserialize(defaultName);
        defaultSerializer.Deserialize(defaultGenericName);
        legacySerializer.Deserialize(legacyName);
    }

    [Benchmark]
    public string Default_Serialize() => defaultSerializer.Serialize(typeof(SmallMessage));

    [Benchmark]
    public Type Default_Deserialize() => defaultSerializer.Deserialize(defaultName);

    [Benchmark]
    public Type Default_Deserialize_Generic() => defaultSerializer.Deserialize(defaultGenericName);

    [Benchmark]
    public string Legacy_Serialize() => legacySerializer.Serialize(typeof(SmallMessage));

    [Benchmark]
    public Type Legacy_Deserialize() => legacySerializer.Deserialize(legacyName);
}
