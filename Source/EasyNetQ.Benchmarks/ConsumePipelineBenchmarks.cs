using BenchmarkDotNet.Attributes;
using EasyNetQ.Benchmarks.Fixtures;
using EasyNetQ.Consumer;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Full in-process consume hot path (see <see cref="ConsumePipelineFixture" />).
/// </summary>
[MemoryDiagnoser]
public class ConsumePipelineBenchmarks
{
    private ConsumeDelegate consumeDelegate = null!;
    private ConsumeContext smallContext;
    private ConsumeContext mediumContext;
    private ConsumeContext largeContext;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var fixture = new ConsumePipelineFixture();
        consumeDelegate = fixture.ConsumeDelegate;
        smallContext = fixture.CreateContext(SampleMessages.CreateSmall());
        mediumContext = fixture.CreateContext(SampleMessages.CreateMedium());
        largeContext = fixture.CreateContext(SampleMessages.CreateLarge());
    }

    [Benchmark]
    public ValueTask<AckStrategyAsync> Consume_Small() => consumeDelegate(smallContext);

    [Benchmark]
    public ValueTask<AckStrategyAsync> Consume_Medium() => consumeDelegate(mediumContext);

    [Benchmark]
    public ValueTask<AckStrategyAsync> Consume_Large() => consumeDelegate(largeContext);
}
