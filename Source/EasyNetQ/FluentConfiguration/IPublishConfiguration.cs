using System.Collections.Generic;

namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allow fluent configuration for Publish
    /// </summary>
    /// <typeparam name="T">The message type to publish</typeparam>
    public interface IPublishConfiguration<T>
    {
        /// <summary>
        /// Add a topic for message publish
        /// </summary>
        /// <param name="topic">The topic to add</param>
        /// <returns></returns>
        IPublishConfiguration<T> WithTopic(string topic);         
    }

    public class PublishConfiguration<T> : IPublishConfiguration<T>
    {
        public IList<string> Topics { get; private set; }

        public PublishConfiguration()
        {
            Topics = new List<string>();
        }

        public IPublishConfiguration<T> WithTopic(string topic)
        {
            Topics.Add(topic);
            return this;
        }
    }
}