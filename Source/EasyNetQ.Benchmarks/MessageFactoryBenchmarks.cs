using BenchmarkDotNet.Attributes;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Cost of materialising <see cref="IMessage" /> from a runtime <see cref="Type" /> (reflection-built delegate)
///     versus a direct generic construction.
/// </summary>
[MemoryDiagnoser]
public class MessageFactoryBenchmarks
{
    private readonly SmallMessage body = SampleMessages.CreateSmall();
    private readonly MessageProperties properties = new() { Type = "type", CorrelationId = "correlation" };

    [GlobalSetup]
    public void GlobalSetup() => MessageFactory.CreateInstance(typeof(SmallMessage), body, properties);

    [Benchmark(Baseline = true)]
    public IMessage Direct_New() => new Message<SmallMessage>(body, properties);

    [Benchmark]
    public IMessage MessageFactory_CreateInstance() => MessageFactory.CreateInstance(typeof(SmallMessage), body, properties);
}
