using System;
using EasyNetQ.Persistent;

namespace EasyNetQ;

/// <summary>
///     The arguments of Blocked event
/// </summary>
public class BlockedEventArgs : EventArgs
{
    /// <summary>
    ///     Creates BlockedEventArgs
    /// </summary>
    public BlockedEventArgs(PersistentConnectionType type, string reason)
    {
        Type = type;
        Reason = reason;
    }

    /// <summary>
    ///     The type of associated connection
    /// </summary>
    public PersistentConnectionType Type { get; }

    /// <summary>
    ///     The reason of the blocking
    /// </summary>
    public string Reason { get; }
}
