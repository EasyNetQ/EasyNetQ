using System;
using EasyNetQ.Persistent;

namespace EasyNetQ;

/// <summary>
///     Arguments of Connected event
/// </summary>
public class ConnectedEventArgs : EventArgs
{
    /// <summary>
    ///     Creates ConnectedEventArgs
    /// </summary>
    public ConnectedEventArgs(PersistentConnectionType type, string hostname, int port)
    {
        Type = type;
        Hostname = hostname;
        Port = port;
    }

    /// <summary>
    ///     The type of associated connection
    /// </summary>
    public PersistentConnectionType Type { get; }

    /// <summary>
    ///     The hostname of the connected endpoint
    /// </summary>
    public string Hostname { get; }

    /// <summary>
    ///     The port of the connected endpoint
    /// </summary>
    public int Port { get; }
}
