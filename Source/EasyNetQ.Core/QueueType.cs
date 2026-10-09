namespace EasyNetQ;

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

    /// <summary>
    ///     Streams are a new persistent and replicated data structure which models an append-only log with non-destructive consumer semantics.
    /// </summary>
    public const string Stream = "stream";
}
