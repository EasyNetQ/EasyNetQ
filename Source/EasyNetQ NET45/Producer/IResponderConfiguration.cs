namespace EasyNetQ.Producer
{
    public interface IResponderConfiguration
    {
        IResponderConfiguration WithPrefetchCount(ushort prefetchCount);
    }

    public class ResponderConfiguration : IResponderConfiguration
    {
        public ResponderConfiguration(ushort defaultPrefetchCount)
        {
            PrefetchCount = defaultPrefetchCount;
        }

        public ushort PrefetchCount { get; private set; }

        public IResponderConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }
    }
}