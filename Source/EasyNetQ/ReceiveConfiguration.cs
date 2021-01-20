using System.Collections.Generic;

namespace EasyNetQ
{
    /// <summary>
    /// Allows subscription configuration to be fluently extended without adding overloads
    ///
    /// e.g.
    /// x => x.WithTopic("*.brighton")
    /// </summary>
    public interface IReceiveConfiguration
    {
        /// <summary>
        /// Configures the queue as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
        /// </summary>
        /// <param name="autoDelete">Queue's durability flag</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithAutoDelete(bool autoDelete = true);

        /// <summary>
        /// Configures the queue's durability
        /// </summary>
        /// <param name="durable">Queue's durability flag</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithDurable(bool durable = true);

        /// <summary>
        /// Configures the consumer's priority
        /// </summary>
        /// <param name="priority">Consumer's priority value</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithPriority(int priority);

        /// <summary>
        /// Configures the consumer's prefetch count
        /// </summary>
        /// <param name="prefetchCount">Consumer's prefetch count value</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithPrefetchCount(ushort prefetchCount);

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
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithExpires(int expires);

        /// <summary>
        /// Configures the consumer's to be exclusive
        /// </summary>
        /// <param name="isExclusive">Consumer's exclusive flag</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration AsExclusive(bool isExclusive = true);

        /// <summary>
        /// Configures the queue's maxPriority
        /// </summary>
        /// <param name="priority">Queue's maxPriority value</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithMaxPriority(byte priority);

        /// <summary>
        /// Sets the maximum number of ready messages that may exist on the queue.
        /// Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached.
        /// </summary>
        /// <param name="maxLength">The maximum number of ready messages that may exist on the queue.</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithMaxLength(int maxLength);

        /// <summary>
        /// Sets the maximum size of the queue in bytes.
        /// Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached
        /// </summary>
        /// <param name="maxLengthBytes">The maximum size of the queue in bytes.</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithMaxLengthBytes(int maxLengthBytes);

        /// <summary>
        /// Sets the queue mode. Valid modes are "default" and "lazy". Works with RabbitMQ version 3.6+.
        /// </summary>
        /// <param name="queueMode">Desired queue mode.</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithQueueMode(string queueMode = QueueMode.Default);

        /// <summary>
        /// Configure the queue as single active consumer. Single active consumer allows to have only one consumer at a time consuming from a queue and to fail over to another registered consumer in case the active one is cancelled or dies.
        /// </summary>
        /// <param name="singleActiveConsumer">Queue's single-active-consumer flag</param>
        /// <returns>Returns a reference to itself</returns>
        IReceiveConfiguration WithSingleActiveConsumer(bool singleActiveConsumer = true);
    }

    internal class ReceiveConfiguration : IReceiveConfiguration
    {
        public bool AutoDelete { get; private set; }
        public int Priority { get; private set; }
        public ushort PrefetchCount { get; private set; }
        public int? Expires { get; private set; }
        public bool IsExclusive { get; private set; }
        public byte? MaxPriority { get; private set; }
        public bool Durable { get; private set; }
        public int? MaxLength { get; private set; }
        public int? MaxLengthBytes { get; private set; }
        public string QueueMode { get; private set; }
        public bool SingleActiveConsumer { get; private set; }

        public ReceiveConfiguration(ushort defaultPrefetchCount)
        {
            AutoDelete = false;
            Priority = 0;
            PrefetchCount = defaultPrefetchCount;
            IsExclusive = false;
            Durable = true;
            SingleActiveConsumer = false;
        }

        public IReceiveConfiguration WithAutoDelete(bool autoDelete = true)
        {
            AutoDelete = autoDelete;
            return this;
        }

        public IReceiveConfiguration WithDurable(bool durable = true)
        {
            Durable = durable;
            return this;
        }

        public IReceiveConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }


        public IReceiveConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        public IReceiveConfiguration WithExpires(int expires)
        {
            Expires = expires;
            return this;
        }

        public IReceiveConfiguration AsExclusive(bool isExclusive = true)
        {
            IsExclusive = isExclusive;
            return this;
        }

        public IReceiveConfiguration WithMaxPriority(byte priority)
        {
            MaxPriority = priority;
            return this;
        }

        public IReceiveConfiguration WithMaxLength(int maxLength)
        {
            MaxLength = maxLength;
            return this;
        }

        public IReceiveConfiguration WithMaxLengthBytes(int maxLengthBytes)
        {
            MaxLengthBytes = maxLengthBytes;
            return this;
        }

        public IReceiveConfiguration WithQueueMode(string queueMode)
        {
            QueueMode = queueMode;
            return this;
        }

        public IReceiveConfiguration WithSingleActiveConsumer(bool singleActiveConsumer = true)
        {
            SingleActiveConsumer = singleActiveConsumer;
            return this;
        }
    }
}
