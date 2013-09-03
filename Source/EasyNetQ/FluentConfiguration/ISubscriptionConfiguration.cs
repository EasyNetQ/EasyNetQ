using System.Collections.Generic;

namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithTopic("*.brighton")
    /// </summary>
    /// <typeparam name="T">The message type to be published</typeparam>
    public interface ISubscriptionConfiguration<T>
    {
        /// <summary>
        /// Add a topic for the queue binding
        /// </summary>
        /// <param name="topic">The topic to add</param>
        /// <returns></returns>
        ISubscriptionConfiguration<T> WithTopic(string topic);
    }

    public class SubscriptionConfiguration<T> : ISubscriptionConfiguration<T>
    {
        public IList<string> Topics { get; private set; }

        public SubscriptionConfiguration()
        {
            Topics = new List<string>();
        }

        public ISubscriptionConfiguration<T> WithTopic(string topic)
        {
            Topics.Add(topic);
            return this;
        }
    }
}