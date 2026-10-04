using BenchmarkDotNet.Attributes;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Cost of materialising the <see cref="IMessage" /> envelope from a runtime <see cref="Type" /> through the
///     registry descriptor (which replaced the expression-compiled MessageFactory) versus direct construction
/// </summary>
[MemoryDiagnoser]
public class MessageFactoryBenchmarks
{
    private readonly SmallMessage body = SampleMessages.CreateSmall();
    private readonly MessageProperties properties = new() { Type = "type", CorrelationId = "correlation" };
    private MessageTypeDescriptor descriptor = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());
        descriptor = registry.GetOrAdd(typeof(SmallMessage));
    }

    [Benchmark(Baseline = true)]
    public IMessage Direct_New() => new Message<SmallMessage>(body, properties);

    [Benchmark]
    public IMessage Descriptor_CreateMessage() => descriptor.CreateMessage(body, properties);
}
