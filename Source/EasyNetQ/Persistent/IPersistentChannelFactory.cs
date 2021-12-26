namespace EasyNetQ.Persistent;

/// <summary>
///     An options for PersistentChannel
/// </summary>
public readonly struct PersistentChannelOptions
{
    /// <summary>
    ///     Creates an options for PersistentChannel
    /// </summary>
    /// <param name="publisherConfirms"></param>
    public PersistentChannelOptions(bool publisherConfirms)
    {
        PublisherConfirms = publisherConfirms;
    }

    /// <summary>
    ///     Enables publisher confirms
    /// </summary>
    public bool PublisherConfirms { get; }
}

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
