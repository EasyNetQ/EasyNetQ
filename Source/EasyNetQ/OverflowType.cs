namespace EasyNetQ
{
    /// <summary>
    ///     Represents a queue overflow type
    /// </summary>
    public static class OverflowType
    {
        /// <summary>
        ///     Default queue overflow mode, the oldest messages will be deleted
        /// </summary>
        public const string Default = "drop-head";

        /// <summary>
        ///     New messages will be rejected
        /// </summary>
        public const string RejectPublish = "reject-publish";
    }
}
