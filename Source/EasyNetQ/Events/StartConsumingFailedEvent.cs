using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Events
{
    /// <summary>
    /// This event is fired when the consumer cannot start consuming successfully.
    /// </summary>
    public class StartConsumingFailedEvent
    {
        public IConsumer Consumer { get; private set; }

        public IQueue Queue { get; private set; }

        public StartConsumingFailedEvent(IConsumer consumer, IQueue queue)
        {
            Consumer = consumer;
            Queue = queue;
        }
    }
}