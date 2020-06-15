using System;

namespace EasyNetQ
{
    /// <summary>
    /// Allows responder configuration to be fluently extended without adding overloads
    ///
    /// e.g.
    /// x => x.WithPrefetchCount(50)
    /// </summary>
    public interface IResponderConfiguration
    {
        /// <summary>
        /// Configures the consumer's prefetch count
        /// </summary>
        /// <param name="prefetchCount">Consumer's prefetch count value</param>
        /// <returns>Reference to the same <see cref="IResponderConfiguration"/> to allow methods chaining</returns>
        IResponderConfiguration WithPrefetchCount(ushort prefetchCount);

        /// <summary>
        /// Sets the queue name
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns>Reference to the same <see cref="IResponderConfiguration"/> to allow methods chaining</returns>
        IResponderConfiguration WithQueueName(string queueName);

        /// <summary>
        /// Configures the queue's durability
        /// </summary>
        /// <returns>Reference to the same <see cref="IResponderConfiguration"/> to allow methods chaining</returns>
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
        /// <param name="expires">The value of the x-expires argument or expires policy describes the expiration period and is subject to the same constraints as x-message-ttl and cannot be zero. Thus a value of 1 means a queue which is unused for 1 second will be deleted.</param>
        /// <returns>Reference to the same <see cref="IResponderConfiguration"/> to allow methods chaining</returns>
        IResponderConfiguration WithExpires(TimeSpan expires);

        /// <summary>
        /// Configures the queue's maxPriority
        /// </summary>
        /// <param name="priority">Queue's maxPriority value</param>
        /// <returns>Reference to the same <see cref="IResponderConfiguration"/> to allow methods chaining</returns>
        IResponderConfiguration WithMaxPriority(byte priority);
    }

    internal class ResponderConfiguration : IResponderConfiguration
    {
        public ResponderConfiguration(ushort defaultPrefetchCount)
        {
            PrefetchCount = defaultPrefetchCount;
        }

        public ushort PrefetchCount { get; private set; }
        public string QueueName { get; private set; }
        public bool Durable { get; private set; } = true;
        public TimeSpan? Expires { get; private set; }
        public byte? MaxPriority { get; private set; }

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

        public IResponderConfiguration WithExpires(TimeSpan expires)
        {
            Expires = expires;
            return this;
        }

        public IResponderConfiguration WithMaxPriority(byte priority)
        {
            MaxPriority = MaxPriority;
            return this;
        }
    }
}
