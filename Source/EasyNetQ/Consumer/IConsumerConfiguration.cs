namespace EasyNetQ.Consumer
{
    public interface IConsumerConfiguration
    {
        int Priority { get; }
        bool CancelOnHaFailover { get; }
        ushort PrefetchCount { get; }
        bool IsExclusive { get; }

        IConsumerConfiguration WithPriority(int priority);
        IConsumerConfiguration WithCancelOnHaFailover(bool cancelOnHaFailover = true);
        IConsumerConfiguration WithPrefetchCount(ushort prefetchCount);
        IConsumerConfiguration AsExclusive();
    }

    public class ConsumerConfiguration : IConsumerConfiguration
    {
        public ConsumerConfiguration(ushort defaultPrefetchCount)
        {
            Priority = 0;
            CancelOnHaFailover = false;
            PrefetchCount = defaultPrefetchCount;
            IsExclusive = false;
        }

        public int Priority { get; private set; }
        public bool IsExclusive { get; private set; }
        public bool CancelOnHaFailover { get; private set; }
        public ushort PrefetchCount { get; private set; }

        public IConsumerConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public IConsumerConfiguration WithCancelOnHaFailover(bool cancelOnHaFailover = true)
        {
            CancelOnHaFailover = cancelOnHaFailover;
            return this;
        }

        public IConsumerConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        public IConsumerConfiguration AsExclusive()
        {
            IsExclusive = true;
            return this;
        }
    }
}