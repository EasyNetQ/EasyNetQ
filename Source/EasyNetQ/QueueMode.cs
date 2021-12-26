namespace EasyNetQ;

/// <summary>
///     Represents a queue mode
/// </summary>
public static class QueueMode
{
    /// <summary>
    ///     Vanilla queue mode, the queue will keep an in-memory cache to deliver messages as fast as possible.
    /// </summary>
    public const string Default = "default";

    /// <summary>
    ///     Lazy queue, which moves messages to disk as earlier as possible to reduce RAM usage.
    /// </summary>
    public const string Lazy = "lazy";
}
