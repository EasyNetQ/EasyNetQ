using BenchmarkDotNet.Attributes;
using EasyNetQ.Benchmarks.Fixtures;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     In-process publish hot path up to the transport boundary (see <see cref="PublishPipelineFixture" />).
/// </summary>
[MemoryDiagnoser]
public class PublishPipelineBenchmarks
{
    private PublishPipelineFixture fixture = null!;
    private SmallMessage small = null!;
    private MediumMessage medium = null!;
    private LargeMessage large = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        fixture = new PublishPipelineFixture();
        small = SampleMessages.CreateSmall();
        medium = SampleMessages.CreateMedium();
        large = SampleMessages.CreateLarge();
    }

    [Benchmark]
    public ValueTask Advanced_Small() => fixture.PublishAdvanced(small);

    [Benchmark]
    public ValueTask Advanced_Medium() => fixture.PublishAdvanced(medium);

    [Benchmark]
    public ValueTask Advanced_Large() => fixture.PublishAdvanced(large);

    [Benchmark]
    public ValueTask PubSub_Small() => fixture.PublishPubSub(small);

    [Benchmark]
    public ValueTask PubSub_Medium() => fixture.PublishPubSub(medium);

    [Benchmark]
    public ValueTask PubSub_Large() => fixture.PublishPubSub(large);
}
