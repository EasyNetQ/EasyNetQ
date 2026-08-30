using BenchmarkDotNet.Attributes;
using EasyNetQ.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     <see cref="IEventBus.PublishAsync{TEvent}" /> is invoked 2–3 times per delivered message
///     (<see cref="DeliveredMessageEvent" />, <see cref="AckEvent" />) and once per published message.
/// </summary>
[MemoryDiagnoser]
public class EventBusBenchmarks
{
    private readonly EventBus emptyBus = new(NullLogger<EventBus>.Instance);
    private readonly EventBus subscribedBus = new(NullLogger<EventBus>.Instance);
    private DeliveredMessageEvent @event;

    [GlobalSetup]
    public void GlobalSetup()
    {
        subscribedBus.Subscribe<DeliveredMessageEvent>(_ => Task.CompletedTask);
        @event = new DeliveredMessageEvent(
            new MessageReceivedInfo("consumer", 1UL, false, "exchange", "routing.key", "queue"),
            new MessageProperties { Type = "type" },
            ReadOnlyMemory<byte>.Empty
        );
    }

    [Benchmark]
    public Task Publish_NoSubscribers() => emptyBus.PublishAsync(@event);

    [Benchmark]
    public Task Publish_OneSubscriber() => subscribedBus.PublishAsync(@event);
}
