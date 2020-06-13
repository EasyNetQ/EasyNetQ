namespace EasyNetQ
{
    /// <summary>
    /// Allows publish configuration to be fluently extended without adding overloads
    ///
    /// e.g.
    /// x => x.WithTopic("*.brighton").WithPriority(2)
    /// </summary>
    public interface IPublishConfiguration
    {
        /// <summary>
        /// Sets a priority of the message
        /// </summary>
        /// <param name="priority">The priority to set</param>
        /// <returns>IPublishConfiguration</returns>
        IPublishConfiguration WithPriority(byte priority);

        /// <summary>
        /// Sets a topic for the message
        /// </summary>
        /// <param name="topic">The topic to set</param>
        /// <returns>IPublishConfiguration</returns>
        IPublishConfiguration WithTopic(string topic);

        /// <summary>
        /// Sets a TTL for the message
        /// </summary>
        /// <param name="expires">The TTL to set in milliseconds</param>
        /// <returns>IPublishConfiguration</returns>
        IPublishConfiguration WithExpires(int expires);
    }

    internal class PublishConfiguration : IPublishConfiguration
    {
        public PublishConfiguration(string defaultTopic)
        {
            Preconditions.CheckNotNull(defaultTopic, "defaultTopic");

            Topic = defaultTopic;
        }

        public IPublishConfiguration WithPriority(byte priority)
        {
            Priority = priority;
            return this;
        }

        public IPublishConfiguration WithTopic(string topic)
        {
            Preconditions.CheckNotNull(topic, "topic");

            Topic = topic;
            return this;
        }

        public IPublishConfiguration WithExpires(int expires)
        {
            Expires = expires;
            return this;
        }

        public byte? Priority { get; private set; }
        public string Topic { get; private set; }
        public int? Expires { get; private set; }
    }
}
