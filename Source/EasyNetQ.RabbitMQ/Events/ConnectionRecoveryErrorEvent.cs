using EasyNetQ.Persistent;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised when an automatic connection recovery attempt fails; the client keeps retrying
///     on its recovery interval, so this can fire repeatedly while the broker is unreachable
/// </summary>
/// <param name="Type">The type of the associated connection</param>
/// <param name="Exception">The recovery failure</param>
public readonly record struct ConnectionRecoveryErrorEvent(PersistentConnectionType Type, Exception Exception);
