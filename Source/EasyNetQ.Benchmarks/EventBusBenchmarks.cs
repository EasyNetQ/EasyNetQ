using BenchmarkDotNet.Attributes;
using EasyNetQ.Events;
using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     <see cref="IEventBus.PublishAsync{TEvent}" /> cost per lifecycle event
///     (it is no longer invoked per message since Phase 1)
/// </summary>
[MemoryDiagnoser]
public class EventBusBenchmarks
{
    private readonly EventBus emptyBus = new(NullLogger<EventBus>.Instance);
    private readonly EventBus subscribedBus = new(NullLogger<EventBus>.Instance);
    private ConnectionCreatedEvent @event;

    [GlobalSetup]
    public void GlobalSetup()
    {
        subscribedBus.Subscribe<ConnectionCreatedEvent>(_ => Task.CompletedTask);
        @event = new ConnectionCreatedEvent(PersistentConnectionType.Producer, new AmqpTcpEndpoint("localhost"));
    }

    [Benchmark]
    public Task Publish_NoSubscribers() => emptyBus.PublishAsync(@event);

    [Benchmark]
    public Task Publish_OneSubscriber() => subscribedBus.PublishAsync(@event);
}
