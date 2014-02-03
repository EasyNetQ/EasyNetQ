using System.Collections.Generic;

namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithTopic("*.brighton")
    /// </summary>
    public interface ISubscriptionConfiguration
    {
        /// <summary>
        /// Add a topic for the queue binding
        /// </summary>
        /// <param name="topic">The topic to add</param>
        /// <returns></returns>
        ISubscriptionConfiguration WithTopic(string topic);
        
        /// <summary>
        /// Indicate if queues for this subscription are declared durable (default:true)
        /// </summary>
        ISubscriptionConfiguration Durable(bool durable);

        /// <summary>
        /// Automatically delete queues after disconnect
        /// </summary>
        ISubscriptionConfiguration AutoDelete(bool autoDelete);
    }

    public class SubscriptionConfiguration : ISubscriptionConfiguration
    {
        public IList<string> Topics { get; private set; }

        public bool Durable { get; set; }
        
        public bool AutoDelete { get; set; }

        public SubscriptionConfiguration()
        {
            Topics = new List<string>();
            Durable = true;
            AutoDelete = false;
        }

        ISubscriptionConfiguration ISubscriptionConfiguration.WithTopic(string topic)
        {
            Topics.Add(topic);
            return this;
        }

        ISubscriptionConfiguration ISubscriptionConfiguration.Durable(bool durable)
        {
            Durable = durable;
            return this;
        }

        ISubscriptionConfiguration ISubscriptionConfiguration.AutoDelete(bool autoDelete)
        {
            AutoDelete = autoDelete;
            return this;
        }
    }
}