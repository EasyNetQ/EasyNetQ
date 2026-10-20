using BenchmarkDotNet.Attributes;
using EasyNetQ.Benchmarks.Fixtures;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Consume pipeline plumbing with a no-op terminal (see <see cref="PipelineOverheadFixture" />)
/// </summary>
[MemoryDiagnoser]
public class PipelineOverheadBenchmarks
{
    private PipelineOverheadFixture fixture = null!;

    [GlobalSetup]
    public void GlobalSetup() => fixture = new PipelineOverheadFixture();

    [Benchmark]
    public ValueTask Consume_Noop() => fixture.ConsumeNoopAsync();
}
