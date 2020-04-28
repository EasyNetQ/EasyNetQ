namespace EasyNetQ.Producer
{
    /// <summary>
    /// Allows configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithPrefetchCount(50)
    /// </summary>
    public interface IResponderConfiguration
    {
        IResponderConfiguration WithPrefetchCount(ushort prefetchCount);

        /// <summary>
        /// Sets the queue name
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns>Reference to the same <see cref="IResponderConfiguration"/> to allow methods chaining.</returns>
        IResponderConfiguration WithQueueName(string queueName);

        /// <summary>
        /// Configures the queue's durability
        /// </summary>
        /// <returns>Reference to the same <see cref="IResponderConfiguration"/> to allow methods chaining.</returns>
        IResponderConfiguration WithDurable(bool durable = true);

        /// <summary>
        /// Expiry time can be set for a given queue by setting the x-expires argument to queue.declare, or by setting the expires policy. 
        /// This controls for how long a queue can be unused before it is automatically deleted. 
        /// Unused means the queue has no consumers, the queue has not been redeclared, and basic.get has not been invoked for a duration of at least the expiration period. 
        /// This can be used, for example, for RPC-style reply queues, where many queues can be created which may never be drained.
        /// The server guarantees that the queue will be deleted, if unused for at least the expiration period. 
        /// No guarantee is given as to how promptly the queue will be removed after the expiration period has elapsed. 
        /// Leases of durable queues restart when the server restarts.
        /// </summary>
        /// <param name="expires">The value of the x-expires argument or expires policy describes the expiration period in milliseconds and is subject to the same constraints as x-message-ttl and cannot be zero. Thus a value of 1000 means a queue which is unused for 1 second will be deleted.</param>
        /// <returns>Reference to the same <see cref="IResponderConfiguration"/> to allow methods chaining.</returns>
        IResponderConfiguration WithExpires(int expires);
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

        public IResponderConfiguration WithExpires(int expires)
        {
            Expires = expires;
            return this;
        }
    }
}
