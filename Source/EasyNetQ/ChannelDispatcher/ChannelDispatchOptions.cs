using EasyNetQ.Persistent;

namespace EasyNetQ.ChannelDispatcher
{

    /// <summary>
    /// A dispatch options of channel
    /// </summary>
    public readonly struct ChannelDispatchOptions
    {
        /// <summary>
        ///     Options for topology operations on producer side
        /// </summary>
        public static readonly ChannelDispatchOptions ProducerTopology = new(
            "Topology", PersistentConnectionType.Producer
        );

        /// <summary>
        ///     Options for publish without confirms
        /// </summary>
        public static readonly ChannelDispatchOptions ProducerPublish = new(
            "Publish", PersistentConnectionType.Producer
        );

        /// <summary>
        ///     Options for publish confirms
        /// </summary>
        public static readonly ChannelDispatchOptions ProducerPublishWithConfirms = new(
            "PublishWithConfirms", PersistentConnectionType.Producer, true
        );

        /// <summary>
        ///     Options for topology operations on producer side
        /// </summary>
        public static readonly ChannelDispatchOptions ConsumerTopology = new(
            "Topology", PersistentConnectionType.Consumer
        );

        /// <summary>
        /// Creates ChannelDispatchOptions
        /// </summary>
        private ChannelDispatchOptions(string name, PersistentConnectionType connectionType, bool publisherConfirms = false)
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
}
