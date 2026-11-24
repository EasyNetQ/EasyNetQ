using EasyNetQ.Consumer;

namespace EasyNetQ.Events;

/// <summary>
/// This event is fired when the logical consumer stops consuming.
///
/// This is _not_ fired when a connection interruption causes EasyNetQ to re-create
/// a PersistentConsumer.
/// </summary>
/// <param name="Consumer">The consumer</param>
public readonly record struct StoppedConsumingEvent(IConsumer Consumer);
