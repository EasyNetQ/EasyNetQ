namespace EasyNetQ.Producer
{
    public interface IResponderConfiguration
    {
        IResponderConfiguration WithPriority(int priority);
        IResponderConfiguration WithPrefetchCount(ushort prefetchCount);
        IResponderConfiguration WithQueueName(string queueName);
    }

    public class ResponderConfiguration : IResponderConfiguration
    {
        public ResponderConfiguration(ushort defaultPrefetchCount)
        {
            Priority = 0;
            PrefetchCount = defaultPrefetchCount;
        }

        public int Priority { get; private set; }
        public ushort PrefetchCount { get; private set; }
        public string QueueName { get; private set; }

        public IResponderConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public IResponderConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        public IResponderConfiguration WithQueueName(string queueName)
        {
            QueueName = queueName;
            return this;
        }
    }
}
