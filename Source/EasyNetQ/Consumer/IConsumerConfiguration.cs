namespace EasyNetQ.Consumer
{
    public interface IConsumerConfiguration
    {
        int Priority { get; }
        ushort PrefetchCount { get; }
        bool IsExclusive { get; }

        IConsumerConfiguration WithPriority(int priority);
        IConsumerConfiguration WithPrefetchCount(ushort prefetchCount);
        IConsumerConfiguration AsExclusive();
    }

    public class ConsumerConfiguration : IConsumerConfiguration
    {
        public ConsumerConfiguration(ushort defaultPrefetchCount)
        {
            Priority = 0;
            PrefetchCount = defaultPrefetchCount;
            IsExclusive = false;
        }

        public int Priority { get; private set; }
        public bool IsExclusive { get; private set; }
        public ushort PrefetchCount { get; private set; }

        public IConsumerConfiguration WithPriority(int priority)
        {
            Priority = priority;
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

        public override string ToString()
        {
            return $"[Priority={Priority}, IsExclusive={IsExclusive}, PrefetchCount={PrefetchCount}]";
        }
    }
}