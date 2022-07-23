using EasyNetQ.Persistent;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after an unblock of the connection
/// </summary>
public readonly struct ConnectionUnblockedEvent
{
    /// <summary>
    ///     The type of the associated connection
    /// </summary>
    public PersistentConnectionType Type { get; }

    public ConnectionUnblockedEvent(PersistentConnectionType type)
    {
        Type = type;
    }
}
