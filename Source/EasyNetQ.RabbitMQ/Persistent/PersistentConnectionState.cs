namespace EasyNetQ.Persistent;

/// <summary>
/// A connection state
/// </summary>
public enum PersistentConnectionState
{
    /// <summary>
    /// Indicates that the connection is not initialised
    /// </summary>
    NotInitialised,
    /// <summary>
    /// Indicates that the connection is initialised and connected
    /// </summary>
    Connected,
    /// <summary>
    /// Indicates that the connection is initialised, but disconnected
    /// </summary>
    Disconnected
}
