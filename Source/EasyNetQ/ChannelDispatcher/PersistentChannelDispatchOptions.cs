using EasyNetQ.Persistent;

namespace EasyNetQ.ChannelDispatcher;

/// <summary>
/// A dispatch options of channel
/// </summary>
public readonly struct PersistentChannelDispatchOptions
{
    /// <summary>
    ///     Options for topology operations on producer side
    /// </summary>
    public static readonly PersistentChannelDispatchOptions ProducerTopology = new(
        "Topology", PersistentConnectionType.Producer
    );

    /// <summary>
    ///     Options for publish without confirms
    /// </summary>
    public static readonly PersistentChannelDispatchOptions ProducerPublish = new(
        "Publish", PersistentConnectionType.Producer
    );

    /// <summary>
    ///     Options for publish confirms
    /// </summary>
    public static readonly PersistentChannelDispatchOptions ProducerPublishWithConfirms = new(
        "PublishWithConfirms", PersistentConnectionType.Producer, true
    );

    /// <summary>
    ///     Options for topology operations on producer side
    /// </summary>
    public static readonly PersistentChannelDispatchOptions ConsumerTopology = new(
        "Topology", PersistentConnectionType.Consumer
    );

    /// <summary>
    /// Creates ChannelDispatchOptions
    /// </summary>
    private PersistentChannelDispatchOptions(string name, PersistentConnectionType connectionType, bool publisherConfirms = false)
    {
        Name = name;
        ConnectionType = connectionType;
        PublisherConfirms = publisherConfirms;
    }

    /// <summary>
    ///     A name associated with channel
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string Name { get; }

    /// <summary>
    ///     A connection type to be used for dispatching
    /// </summary>
    public PersistentConnectionType ConnectionType { get; }

    /// <summary>
    ///     True if publisher confirms are enabled
    /// </summary>
    public bool PublisherConfirms { get; }
}
