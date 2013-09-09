namespace EasyNetQ.Topology
{
    /// <summary>
    /// Represents an AMQP queue
    /// </summary>
    public interface IQueue : IBindable
    {
        /// <summary>
        /// The name of the queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// If this property is true, the queue will be removed after a single use.
        /// Used for request-response return queues.
        /// </summary>
        bool IsSingleUse { get; }

        /// <summary>
        /// Set this queue as single use (see IsSingleUse)
        /// </summary>
        /// <returns></returns>
        IQueue SetAsSingleUse();

        /// <summary>
        /// Is this queue transient?
        /// </summary>
        bool IsExclusive { get; }
    }
}