using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Events
{
    /// <summary>
    /// This event is fired when the consumer starts consuming successfully.
    /// </summary>
    public class StartConsumingSucceededEvent
    {
        public IConsumer Consumer { get; }

        public IQueue Queue { get; }

        public StartConsumingSucceededEvent(IConsumer consumer, IQueue queue)
        {
            Consumer = consumer;
            Queue = queue;
        }
    }
}