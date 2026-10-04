using EasyNetQ.Persistent;

namespace EasyNetQ.Events;

/// <summary>
///     Raised when a connection has been re-established after a loss. Transport-neutral counterpart of
///     <c>ConnectionRecoveredEvent</c> (which carries the AMQP endpoint), consumed by core services such
///     as the RPC client to restore non-durable topology.
/// </summary>
public readonly record struct ConnectionRestoredEvent(PersistentConnectionType Type);
