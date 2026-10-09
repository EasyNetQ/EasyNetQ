using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Events;

/// <summary>
/// This event is fired when the broker cancels consuming from a queue (the queue was deleted, its leader was lost,
/// a policy changed). Unlike a connection interruption, the consumer does not restart that queue on its own.
/// </summary>
/// <param name="Consumer">The consumer</param>
/// <param name="Queue">The cancelled queue</param>
public readonly record struct ConsumerCancelledEvent(IConsumer Consumer, in Queue Queue);
