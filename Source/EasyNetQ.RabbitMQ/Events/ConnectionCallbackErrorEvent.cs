using EasyNetQ.Persistent;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised when an exception escapes a connection event callback; the client swallows such
///     exceptions after raising this, so without observing it they are silently lost
/// </summary>
/// <param name="Type">The type of the associated connection</param>
/// <param name="Exception">The escaped exception</param>
public readonly record struct ConnectionCallbackErrorEvent(PersistentConnectionType Type, Exception Exception);
