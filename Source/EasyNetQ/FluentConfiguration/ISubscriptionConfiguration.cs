using System.Collections.Generic;

namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithTopic("*.brighton")
    /// </summary>
    /// <typeparam name="T">The message type to be published</typeparam>
    public interface ISubscriptionConfiguration
    {
        /// <summary>
        /// Add a topic for the queue binding
        /// </summary>
        /// <param name="topic">The topic to add</param>
        /// <returns></returns>
        ISubscriptionConfiguration WithTopic(string topic);

        /// <summary>
        /// Configures the queue's durability
        /// </summary>
        /// <returns></returns>
        ISubscriptionConfiguration WithAutoDelete(bool autoDelete = true);


        /// <summary>
        /// Configures the consumer's priority
        /// </summary>
        /// <returns></returns>
        ISubscriptionConfiguration WithPriority(int priority);

        /// <summary>
        /// Configures the consumer's x-cancel-on-ha-failover attribute
        /// </summary>
        /// <returns></returns>
        ISubscriptionConfiguration WithCancelOnHaFailover(bool cancelOnHaFailover = true);


        /// <summary>
        /// Configures the consumer's prefetch count
        /// </summary>
        /// <returns></returns>
        ISubscriptionConfiguration WithPrefetchCount(ushort prefetchCount);

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
        /// <returns></returns>
        ISubscriptionConfiguration WithExpires(int expires);

        /// <summary>
        /// Configures the consumer's to be exclusive
        /// </summary>
        /// <returns></returns>
        ISubscriptionConfiguration AsExclusive();
    }

    public class SubscriptionConfiguration : ISubscriptionConfiguration
    {
        public IList<string> Topics { get; private set; }
        public bool AutoDelete { get; private set; }
        public int Priority { get; private set; }
        public bool CancelOnHaFailover { get; private set; }
        public ushort PrefetchCount { get; private set; }
        public int Expires { get; private set; }

        public bool IsExclusive { get; private set; }

        public SubscriptionConfiguration(ushort defaultPrefetchCount)
        {
            Topics = new List<string>();
            AutoDelete = false;
            Priority = 0;
            CancelOnHaFailover = false;
            PrefetchCount = defaultPrefetchCount;
            Expires = int.MaxValue;
            IsExclusive = false;
        }

        public ISubscriptionConfiguration WithTopic(string topic)
        {
            Topics.Add(topic);
            return this;
        }

        public ISubscriptionConfiguration WithAutoDelete(bool autoDelete = true)
        {
            AutoDelete = autoDelete;
            return this;
        }

        public ISubscriptionConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public ISubscriptionConfiguration WithCancelOnHaFailover(bool cancelOnHaFailover = true)
        {
            CancelOnHaFailover = cancelOnHaFailover;
            return this;
        }

        public ISubscriptionConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        public ISubscriptionConfiguration WithExpires(int expires)
        {
            Expires = expires;
            return this;
        }

        public ISubscriptionConfiguration AsExclusive()
        {
            IsExclusive = true;
            return this;
        }
    }
}