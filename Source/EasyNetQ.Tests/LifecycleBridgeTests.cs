using System.Collections.Concurrent;
using EasyNetQ.Configuration;
using EasyNetQ.Events;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;

namespace EasyNetQ.Tests;

/// <summary>
///     The RabbitMQ transport bridges the internal connection events onto the lifecycle pipeline, filtered by
///     the connection the event belongs to; consumers notify Started/Stopped around the transport consumer.
/// </summary>
public class LifecycleBridgeTests
{
    private sealed class TestBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services) : IEasyNetQBuilder
    {
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; } = services;
    }

    [Fact]
    public async Task Should_bridge_connection_events_to_the_lifecycle_pipeline()
    {
        var events = new ConcurrentQueue<(LifecycleLayer Layer, string Event, string? Reason)>();

        await using var mockBuilder = new MockBuilder(x =>
            new TestBuilder(x).Lifecycle(lifecycle => lifecycle.Use("record", (context, next) =>
            {
                events.Enqueue((context.Layer, context.Event.Name, context.Reason));
                return next(context);
            }))
        );

        _ = mockBuilder.Bus; // create the bus so the transport connections subscribe

        await mockBuilder.EventBus.PublishAsync(new ConnectionCreatedEvent(PersistentConnectionType.Producer, new AmqpTcpEndpoint()));
        await mockBuilder.EventBus.PublishAsync(new ConnectionBlockedEvent(PersistentConnectionType.Producer, "low memory"));
        await mockBuilder.EventBus.PublishAsync(new ConnectionDisconnectedEvent(PersistentConnectionType.Consumer, new AmqpTcpEndpoint(), "bye"));

        events.Should().ContainInOrder(
            (LifecycleLayer.Connection, "Connected", null),
            (LifecycleLayer.Connection, "Blocked", "low memory"),
            (LifecycleLayer.Connection, "Disconnected", "bye")
        );
        // each event fired once: the other connection's bridge filtered it out
        events.Count.Should().Be(3);
    }

    [Fact]
    public async Task Should_notify_consumer_started_and_stopped()
    {
        var events = new ConcurrentQueue<(LifecycleLayer Layer, string Event)>();

        await using var mockBuilder = new MockBuilder(x =>
            new TestBuilder(x).Lifecycle(lifecycle => lifecycle.Use("record", (context, next) =>
            {
                events.Enqueue((context.Layer, context.Event.Name));
                return next(context);
            }))
        );

        var disposable = await mockBuilder.Bus.Advanced.ConsumeAsync(
            new Topology.Queue("lifecycle.q"),
            (_, _, _) => Task.FromResult(AckDecision.Ack)
        );
        await disposable.DisposeAsync();

        events.Should().Contain((LifecycleLayer.Consumer, "Started"));
        events.Should().Contain((LifecycleLayer.Consumer, "Stopped"));
    }
}
