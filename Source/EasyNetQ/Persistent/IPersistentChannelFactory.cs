namespace EasyNetQ.Persistent;

/// <summary>
///     An options for PersistentChannel
/// </summary>
public readonly record struct PersistentChannelOptions(bool PublisherConfirms);

/// <summary>
///     Creates PersistentChannel using the connection and the options
/// </summary>
public interface IPersistentChannelFactory
{
    /// <summary>
    ///     Creates PersistentChannel
    /// </summary>
    /// <param name="connection">The connection</param>
    /// <param name="options">The channel options</param>
    /// <returns>New PersistentChannel</returns>
    IPersistentChannel CreatePersistentChannel(IPersistentConnection connection, PersistentChannelOptions options);
}
