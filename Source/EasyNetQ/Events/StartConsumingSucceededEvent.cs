using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Events
{
    /// <summary>
    /// This event is fired when the consumer starts consuming successfully.
    /// </summary>
    public class StartConsumingSucceededEvent
    {
        public IConsumer Consumer { get; private set; }

        public IQueue Queue { get; private set; }

        public StartConsumingSucceededEvent(IConsumer consumer, IQueue queue)
        {
            Consumer = consumer;
            Queue = queue;
        }
    }
}