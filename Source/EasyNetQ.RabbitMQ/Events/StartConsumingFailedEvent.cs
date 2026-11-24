using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Events;

/// <summary>
/// This event is fired when the consumer cannot start consuming successfully.
/// </summary>
public readonly record struct StartConsumingFailedEvent(IConsumer Consumer, in Queue Queue);
