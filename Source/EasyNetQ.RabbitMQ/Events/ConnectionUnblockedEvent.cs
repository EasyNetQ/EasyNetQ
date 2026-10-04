using EasyNetQ.Persistent;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after an unblock of the connection
/// </summary>
/// <param name="Type">The type of the associated connection</param>
public readonly record struct ConnectionUnblockedEvent(PersistentConnectionType Type);
