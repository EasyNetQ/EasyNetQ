﻿using System.Collections.Generic;

namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithTopic("*.brighton")
    /// </summary>
    /// <typeparam name="T">The message type to be published</typeparam>
    public interface ISubscriptionConfiguration
    {
        /// <summary>
        /// Add a topic for the queue binding
        /// </summary>
        /// <param name="topic">The topic to add</param>
        /// <returns></returns>
        ISubscriptionConfiguration WithTopic(string topic);

        /// <summary>
        /// Configures the queue's durability
        /// </summary>
        /// <returns></returns>
        ISubscriptionConfiguration WithAutoDelete(bool autoDelete = true);


        /// <summary>
        /// Configures the consumer's priority
        /// </summary>
        /// <returns></returns>
        ISubscriptionConfiguration WithPriority(int priority);


        /// <summary>
        /// Configures is consumer exclusive or not
        /// </summary>
        /// <returns></returns>
        ISubscriptionConfiguration AsExclusive();
    }

    public class SubscriptionConfiguration : ISubscriptionConfiguration
    {
        public IList<string> Topics { get; private set; }
        public bool AutoDelete { get; private set; }
        public int Priority { get; private set; }
        public bool IsExclusive { get; private set; }

        public SubscriptionConfiguration()
        {
            Topics = new List<string>();
            AutoDelete = false;
            Priority = 0;
            IsExclusive = false;
        }

        public ISubscriptionConfiguration WithAutoDelete(bool autoDelete = true)
        {
            AutoDelete = autoDelete;
            return this;
        }

        public ISubscriptionConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public ISubscriptionConfiguration AsExclusive()
        {
            IsExclusive = true;
            return this;
        }

        public ISubscriptionConfiguration WithTopic(string topic)
        {
            Topics.Add(topic);
            return this;
        }
    }
}