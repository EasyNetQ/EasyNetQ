using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Events;

/// <summary>
/// This event is fired when the consumer starts consuming successfully.
/// </summary>
public readonly record struct StartConsumingSucceededEvent(IConsumer Consumer, in Queue Queue);
