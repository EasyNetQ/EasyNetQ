using EasyNetQ.Persistent;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a block of the connection
/// </summary>
public readonly struct ConnectionBlockedEvent
{
    /// <summary>
    ///     The type of the associated connection
    /// </summary>
    public PersistentConnectionType Type { get; }

    /// <summary>
    ///     The reason of a block
    /// </summary>
    public string Reason { get; }

    /// <summary>
    ///     Creates ConnectionBlockedEvent
    /// </summary>
    public ConnectionBlockedEvent(PersistentConnectionType type, string reason)
    {
        Type = type;
        Reason = reason;
    }
}
