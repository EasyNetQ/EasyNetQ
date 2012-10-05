using System.Collections.Generic;

namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithArgument("x-ha-policy", "all").WithTopic("*.brighton")
    /// </summary>
    /// <typeparam name="T">The message type to be published</typeparam>
    public interface ISubscriptionConfiguration<T>
    {
        /// <summary>
        /// Add an AMQP argument for the subscription consumer
        /// </summary>
        /// <param name="key">Argument key</param>
        /// <param name="value">Argument value</param>
        /// <returns></returns>
        ISubscriptionConfiguration<T> WithArgument(string key, object value);

        /// <summary>
        /// Add a topic for the queue binding
        /// </summary>
        /// <param name="topic">The topic to add</param>
        /// <returns></returns>
        ISubscriptionConfiguration<T> WithTopic(string topic);
    }

    public class SubscriptionConfiguration<T> : ISubscriptionConfiguration<T>
    {
        public IDictionary<string, object> Arguments { get; private set; }
        public IList<string> Topics { get; private set; }

        public SubscriptionConfiguration()
        {
            Arguments = new Dictionary<string, object>();
            Topics = new List<string>();
        }

        public ISubscriptionConfiguration<T> WithArgument(string key, object value)
        {
            Arguments.Add(key, value);
            return this;
        }

        public ISubscriptionConfiguration<T> WithTopic(string topic)
        {
            Topics.Add(topic);
            return this;
        }
    }
}