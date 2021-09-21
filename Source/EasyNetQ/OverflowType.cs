namespace EasyNetQ
{
    /// <summary>
    ///     Represents a queue overflow type
    /// </summary>
    public static class OverflowType
    {
        /// <summary>
        ///     Default queue overflow mode, the oldest messages will be deleted.
        /// </summary>
        public const string Default = "drop-head";

        /// <summary>
        ///     New messages will be rejected.
        /// </summary>
        public const string RejectPublish = "reject-publish";

        /// <summary>
        ///     New messages will be rejected. The difference between reject-publish
        ///     and reject-publish-dlx is that reject-publish-dlx also dead-letters
        ///     rejected messages.
        /// </summary>
        public const string RejectPublishDlx = "reject-publish-dlx";
    }
}
