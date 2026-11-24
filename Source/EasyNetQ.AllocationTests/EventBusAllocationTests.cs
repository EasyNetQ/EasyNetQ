using EasyNetQ.Events;
using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace EasyNetQ.AllocationTests;

public class EventBusAllocationTests
{
    private static readonly ConnectionCreatedEvent Event = new(PersistentConnectionType.Producer, new AmqpTcpEndpoint("localhost"));

    [Fact]
    public void Publish_with_no_subscribers()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        var bytes = AllocationAssert.BytesPerIteration(() => bus.PublishAsync(Event));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.EventBusPublishNoSubscribers);
    }

    [Fact]
    public void Publish_with_one_subscriber()
    {
        var bus = new EventBus(NullLogger<EventBus>.Instance);
        bus.Subscribe<ConnectionCreatedEvent>(_ => Task.CompletedTask);
        var bytes = AllocationAssert.BytesPerIteration(() => bus.PublishAsync(Event));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.EventBusPublishOneSubscriber);
    }
}
