using EasyNetQ.Persistent;

namespace EasyNetQ.ChannelDispatcher;

/// <summary>
///     A dispatch options of channel
/// </summary>
/// <param name="Name">A name associated with channel</param>
/// <param name="ConnectionType">A connection type to be used for dispatching</param>
/// <param name="PublisherConfirms"><see langword="true" /> if publisher confirms are enabled</param>
public readonly record struct PersistentChannelDispatchOptions(string Name, PersistentConnectionType ConnectionType, bool PublisherConfirms = false)
{
    /// <summary>
    ///     Options for topology operations on producer side
    /// </summary>
    public static readonly PersistentChannelDispatchOptions ProducerTopology = new("Topology", PersistentConnectionType.Producer);

    /// <summary>
    ///     Options for publish without confirms
    /// </summary>
    public static readonly PersistentChannelDispatchOptions ProducerPublish = new("Publish", PersistentConnectionType.Producer);

    /// <summary>
    ///     Options for publish confirms
    /// </summary>
    public static readonly PersistentChannelDispatchOptions ProducerPublishWithConfirms = new("PublishWithConfirms", PersistentConnectionType.Producer, true);

    /// <summary>
    ///     Options for topology operations on consumer side
    /// </summary>
    public static readonly PersistentChannelDispatchOptions ConsumerTopology = new("Topology", PersistentConnectionType.Consumer);
}
