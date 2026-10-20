using BenchmarkDotNet.Attributes;
using EasyNetQ.Benchmarks.Fixtures;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Full in-process consume hot path (see <see cref="ConsumePipelineFixture" />).
/// </summary>
[MemoryDiagnoser]
public class ConsumePipelineBenchmarks
{
    private ConsumePipelineFixture fixture = null!;
    private (MessageProperties Properties, ReadOnlyMemory<byte> Body) small;
    private (MessageProperties Properties, ReadOnlyMemory<byte> Body) medium;
    private (MessageProperties Properties, ReadOnlyMemory<byte> Body) large;

    [GlobalSetup]
    public void GlobalSetup()
    {
        fixture = new ConsumePipelineFixture();
        small = fixture.Serialize(SampleMessages.CreateSmall());
        medium = fixture.Serialize(SampleMessages.CreateMedium());
        large = fixture.Serialize(SampleMessages.CreateLarge());
    }

    [Benchmark]
    public ValueTask Consume_Small() => fixture.ConsumeAsync(small.Properties, small.Body);

    [Benchmark]
    public ValueTask Consume_Medium() => fixture.ConsumeAsync(medium.Properties, medium.Body);

    [Benchmark]
    public ValueTask Consume_Large() => fixture.ConsumeAsync(large.Properties, large.Body);
}
