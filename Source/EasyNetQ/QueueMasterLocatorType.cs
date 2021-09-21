namespace EasyNetQ
{
    /// <summary>
    ///     Represents a strategy used to control queue leaders distribution between nodes.
    ///
    ///     Every queue in RabbitMQ has a primary replica. That replica is called queue leader
    ///     (originally "queue master"). All queue operations go through the leader replica
    ///     first and then are replicated to followers (mirrors). This is necessary to guarantee
    ///     FIFO ordering of messages. To avoid some nodes in a cluster hosting the majority of
    ///     queue leader replicas and thus handling most of the load, queue leaders should be
    ///     reasonably evenly distributed across cluster nodes.
    /// </summary>
    public static class QueueMasterLocatorType
    {
        /// <summary>
        ///     Pick the node hosting the minimum number of leaders
        /// </summary>
        public const string MinMasters = "min-masters";

        /// <summary>
        ///     Pick the node the client that declares the queue is connected to
        /// </summary>
        public const string ClientLocal = "client-local";

        /// <summary>
        ///     Pick a random node
        /// </summary>
        public const string Random = "random";
    }
}
