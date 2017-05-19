using System;
using System.Collections.Generic;
using System.Text;

namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows request configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithQueueName("MyQueue")
    /// </summary>
    public interface IRequestConfiguration
    {
        /// <summary>
        /// Sets the queue name to publish to
        /// </summary>
        /// <param name="queueName">The queue name</param>
        /// <returns>IPublishConfiguration</returns>
        IRequestConfiguration WithQueueName(string queueName);
    }

    public class RequestConfiguration : IRequestConfiguration
    {
        public string QueueName { get; private set; }

        public IRequestConfiguration WithQueueName(string queueName)
        {
            QueueName = queueName;
            return this;
        }
    }
}
