namespace EasyNetQ.Persistent;

/// <summary>
///     Represents a status of connection
/// </summary>
/// <param name="Type">The connection type</param>
/// <param name="State">The connection state</param>
/// <param name="ConnectedAt">The date when a connection is established</param>
/// <param name="DisconnectionReason">The reason why a connection is disconnected</param>
public sealed record PersistentConnectionStatus(
    PersistentConnectionType Type,
    PersistentConnectionState State,
    DateTime? ConnectedAt = null,
    string? DisconnectionReason = null
)
{
    internal PersistentConnectionStatus ToUnknown() =>
        new(Type, PersistentConnectionState.Unknown);

    internal PersistentConnectionStatus ToConnected() =>
        new(Type, PersistentConnectionState.Connected, DateTime.UtcNow);

    internal PersistentConnectionStatus ToDisconnected(string? reason) =>
        new(Type, PersistentConnectionState.Disconnected, null, reason);
}
