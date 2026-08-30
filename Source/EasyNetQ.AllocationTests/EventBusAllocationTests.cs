using EasyNetQ.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.AllocationTests;

public class EventBusAllocationTests
{
    private static readonly DeliveredMessageEvent Event = new(
        new MessageReceivedInfo("consumer", 1UL, false, "exchange", "routing.key", "queue"),
        new MessageProperties { Type = "type" },
        ReadOnlyMemory<byte>.Empty
    );

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
        bus.Subscribe<DeliveredMessageEvent>(_ => Task.CompletedTask);
        var bytes = AllocationAssert.BytesPerIteration(() => bus.PublishAsync(Event));
        AllocationAssert.ShouldNotExceed(bytes, Ceilings.EventBusPublishOneSubscriber);
    }
}
