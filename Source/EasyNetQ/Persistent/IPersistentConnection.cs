using System;
using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

/// <summary>
///     An abstraction on top of connection which manages its persistence and allows to open channels
/// </summary>
public interface IPersistentConnection : IDisposable
{
    /// <summary>
    ///     True if a connection is connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Establish a connection
    /// </summary>
    void Connect();

    /// <summary>
    ///     Creates a new channel
    /// </summary>
    /// <returns>New channel</returns>
    IModel CreateModel();
}
