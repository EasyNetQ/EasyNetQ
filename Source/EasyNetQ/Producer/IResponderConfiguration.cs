namespace EasyNetQ.Producer
{
    public interface IResponderConfiguration
    {
        IResponderConfiguration WithPrefetchCount(ushort prefetchCount);
        IResponderConfiguration WithQueueName(string queueName);
    }

    public class ResponderConfiguration : IResponderConfiguration
    {
        public ResponderConfiguration(ushort defaultPrefetchCount)
        {
            PrefetchCount = defaultPrefetchCount;
        }

        public ushort PrefetchCount { get; private set; }
        public string QueueName { get; private set; }

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