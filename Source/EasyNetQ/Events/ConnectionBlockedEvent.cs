using EasyNetQ.Persistent;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a block of the connection
/// </summary>
/// <param name="Type">The type of the associated connection</param>
/// <param name="Reason">The reason of a block</param>
public readonly record struct ConnectionBlockedEvent(PersistentConnectionType Type, string Reason);
