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
    [Obsolete("Use Status instead")]bool IsConnected { get; }

    /// <summary>
    ///     <see langword="true"/> if a connection is connected
    /// </summary>
    PersistentConnectionStatus Status { get; }

    /// <summary>
    ///     Establish a connection
    /// </summary>
    void Connect();

    /// <summary>
    ///     Creates a new channel
    /// </summary>
    /// <returns>New channel</returns>
    Task<IChannel> CreateChannelAsync(CreateChannelOptions? options = null, CancellationToken cancellationToken = default);
}
