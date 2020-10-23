namespace EasyNetQ
{
    /// <summary>
    ///     Represents a queue type
    /// </summary>
    public static class QueueType
    {
        /// <summary>
        ///     Vanilla queue type
        /// </summary>
        public const string Classic = "classic";

        /// <summary>
        ///     Quorum queue, which is durable, replicated FIFO queue based on the Raft
        /// </summary>
        public const string Quorum = "quorum";
    }
}
