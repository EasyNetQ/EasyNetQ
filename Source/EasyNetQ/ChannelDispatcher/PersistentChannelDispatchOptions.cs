namespace EasyNetQ.ChannelDispatcher;

/// <summary>
/// A dispatch options of channel
/// </summary>
public enum PersistentChannelDispatchOptions
{
    /// <summary>
    ///     Options for topology operations on producer side
    /// </summary>
    ProducerTopology = 1,

    /// <summary>
    ///     Options for publish without confirms
    /// </summary>
    ProducerPublish = 2,

    /// <summary>
    ///     Options for publish confirms
    /// </summary>
    ProducerPublishWithConfirms = 3,

    /// <summary>
    ///     Options for topology operations on producer side
    /// </summary>
    ConsumerTopology = 4
}
