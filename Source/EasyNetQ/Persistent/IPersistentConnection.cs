using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

/// <summary>
///     An abstraction on top of connection which manages its persistence and allows to open channels
/// </summary>
public interface IPersistentConnection : IDisposable
{

    /// <summary>
    ///     <see langword="true"/> if a connection is connected
    /// </summary>
    PersistentConnectionStatus Status { get; }

    /// <summary>
    ///     Establish a connection
    /// </summary>
    Task ConnectAsync();

    /// <summary>
    ///     Creates a new channel
    /// </summary>
    /// <returns>New channel</returns>
    Task<IChannel> CreateChannelAsync(CreateChannelOptions options = null, CancellationToken cancellationToken = default);
}
