namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows publish configuration to be fluently extended without adding overloads to IBus
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

    public class PublishConfiguration : IPublishConfiguration
    {
        private readonly string defaultTopic;

        public PublishConfiguration(string defaultTopic)
        {
            Preconditions.CheckNotNull(defaultTopic, "defaultTopic");

            this.defaultTopic = defaultTopic;
        }

        public IPublishConfiguration WithPriority(byte priority)
        {
            Priority = priority;
            return this;
        }

        public IPublishConfiguration WithTopic(string topic)
        {
            Topic = topic;
            return this;
        }

        public IPublishConfiguration WithExpires(int expires)
        {
            Expires = expires;
            return this;
        }

        public byte? Priority { get; private set; }

        private string topic;

        public string Topic
        {
            get { return topic ?? defaultTopic; }
            private set { topic = value; }
        }

        public int? Expires { get; private set; }
    }
}