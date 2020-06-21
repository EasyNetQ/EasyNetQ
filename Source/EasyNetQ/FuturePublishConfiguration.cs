namespace EasyNetQ
{
    /// <summary>
    ///     Allows future publish configuration to be fluently extended without adding overloads
    ///     e.g.
    ///     x => x.WithTopic("*.brighton").WithPriority(2)
    /// </summary>
    public interface IFuturePublishConfiguration
    {
        /// <summary>
        ///     Sets a priority of the message
        /// </summary>
        /// <param name="priority">The priority to set</param>
        IFuturePublishConfiguration WithPriority(byte priority);

        /// <summary>
        ///     Sets a topic for the message
        /// </summary>
        /// <param name="topic">The topic to set</param>
        IFuturePublishConfiguration WithTopic(string topic);

        IFuturePublishConfiguration WithPublisherConfirms(bool publisherConfirms = true);
    }

    internal class FuturePublishConfiguration : IFuturePublishConfiguration
    {
        public FuturePublishConfiguration(string defaultTopic, bool publisherConfirms)
        {
            Preconditions.CheckNotNull(defaultTopic, "defaultTopic");

            Topic = defaultTopic;
            PublisherConfirms = publisherConfirms;
        }

        public byte? Priority { get; private set; }
        public string Topic { get; private set; }
        public bool PublisherConfirms { get; private set; }

        public IFuturePublishConfiguration WithPriority(byte priority)
        {
            Priority = priority;
            return this;
        }

        public IFuturePublishConfiguration WithTopic(string topic)
        {
            Preconditions.CheckNotNull(topic, "topic");

            Topic = topic;
            return this;
        }

        public IFuturePublishConfiguration WithPublisherConfirms(bool publisherConfirms = true)
        {
            PublisherConfirms = publisherConfirms;
            return this;
        }
    }
}
