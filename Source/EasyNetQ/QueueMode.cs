namespace EasyNetQ
{
    /// <summary>
    ///     Represents a queue mode
    /// </summary>
    public static class QueueMode
    {
        /// <summary>
        ///     Vanilla queue mode
        /// </summary>
        public const string Default = "default";

        /// <summary>
        ///     Lazy queue, which moves messages to disk as earlier as possible
        /// </summary>
        public const string Lazy = "lazy";
    }
}
