namespace EasyNetQ.AutoSubscribe;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SubscriptionConfigurationAttribute : Attribute
{
    /// <summary>
    /// Configures the queue as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
    /// </summary>
    public bool AutoDelete { get; set; }

    /// <summary>
    /// Configures the consumer's priority
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Configures the consumer's prefetch count
    /// </summary>
    public ushort PrefetchCount { get; set; }

    /// <summary>
    /// Expiry time can be set for a given queue by setting the x-expires argument to queue.declare, or by setting the expires policy.
    /// This controls for how long a queue can be unused before it is automatically deleted.
    /// Unused means the queue has no consumers, the queue has not been redeclared, and basic.get has not been invoked for a duration of at least the expiration period.
    /// This can be used, for example, for RPC-style reply queues, where many queues can be created which may never be drained.
    /// The server guarantees that the queue will be deleted, if unused for at least the expiration period.
    /// No guarantee is given as to how promptly the queue will be removed after the expiration period has elapsed.
    /// Leases of durable queues restart when the server restarts.
    /// </summary>
    /// <remarks>
    /// The value of the x-expires argument or expires policy describes the expiration period in milliseconds and is subject to the same constraints as x-message-ttl and cannot be zero. Thus a value of 1000 means a queue which is unused for 1 second will be deleted.
    /// </remarks>
    public int Expires { get; set; }

    /// <summary>
    /// Configure the queue as single active consumer. Single active consumer allows to have only one consumer at a time consuming from a queue and to fail over to another registered consumer in case the active one is cancelled or dies.
    /// </summary>
    public bool SingleActiveConsumer { get; set; }

    /// <summary>
    /// Configures the consumer's to be exclusive
    /// </summary>
    public bool AsExclusive { get; set; }
}
