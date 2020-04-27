namespace EasyNetQ.Producer
{
    public interface IResponderConfiguration
    {
        IResponderConfiguration WithPrefetchCount(ushort prefetchCount);

        IResponderConfiguration WithQueueName(string queueName);

        IResponderConfiguration WithDurable(bool durable = true);

        IResponderConfiguration WithExpires(int? expires);
    }

    public class ResponderConfiguration : IResponderConfiguration
    {
        public ResponderConfiguration(ushort defaultPrefetchCount)
        {
            PrefetchCount = defaultPrefetchCount;
        }

        public ushort PrefetchCount { get; private set; }

        public string QueueName { get; private set; }

        /// <summary>
        /// Durable queues remain active when a server restarts.
        /// </summary>
        public bool Durable { get; private set; } = true;

        /// <summary>
        /// Determines how long (in milliseconds) a queue can remain unused before it is automatically deleted by the server.
        /// </summary>
        public int? Expires { get; private set; }

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

        public IResponderConfiguration WithDurable(bool durable = true)
        {
            Durable = durable;
            return this;
        }

        public IResponderConfiguration WithExpires(int? expires)
        {
            Expires = expires;
            return this;
        }
    }
}
