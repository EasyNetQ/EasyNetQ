using System;
using EasyNetQ.Persistent;

namespace EasyNetQ;

/// <summary>
///     The arguments of Unblocked event
/// </summary>
public class UnblockedEventArgs : EventArgs
{
    /// <summary>
    ///     Creates BlockedEventArgs
    /// </summary>
    public UnblockedEventArgs(PersistentConnectionType type)
    {
        Type = type;
    }

    /// <summary>
    ///     The type of the associated connection
    /// </summary>
    public PersistentConnectionType Type { get; }
}
