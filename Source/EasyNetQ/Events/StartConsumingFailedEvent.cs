using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Events;

/// <summary>
/// This event is fired when the consumer cannot start consuming successfully.
/// </summary>
public readonly struct StartConsumingFailedEvent
{
    public IConsumer Consumer { get; }

    public Queue Queue { get; }

    public StartConsumingFailedEvent(IConsumer consumer, in Queue queue)
    {
        Consumer = consumer;
        Queue = queue;
    }
}
