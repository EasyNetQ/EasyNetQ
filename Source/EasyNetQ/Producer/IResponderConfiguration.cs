namespace EasyNetQ.Producer
{
    public interface IResponderConfiguration
    {
        IResponderConfiguration WithPrefetchCount(ushort prefetchCount);
        IResponderConfiguration WithDurable(bool durable);
    }

    public class ResponderConfiguration : IResponderConfiguration
    {
        public ResponderConfiguration(ushort defaultPrefetchCount)
        {
            PrefetchCount = defaultPrefetchCount;
        }

        public ushort PrefetchCount { get; private set; }
        public bool Durable { get; private set; }

        public IResponderConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        public IResponderConfiguration WithDurable(bool durable)
        {
            Durable = durable;
            return this;
        }
    }
}