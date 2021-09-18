using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Events
{
    /// <summary>
    /// This event is fired when the consumer starts consuming successfully.
    /// </summary>
    public readonly struct StartConsumingSucceededEvent
    {
        public IConsumer Consumer { get; }

        public Queue Queue { get; }

        public StartConsumingSucceededEvent(IConsumer consumer, in Queue queue)
        {
            Consumer = consumer;
            Queue = queue;
        }
    }
}
