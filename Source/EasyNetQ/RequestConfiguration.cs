using System;

namespace EasyNetQ
{
    /// <summary>
    /// Allows request configuration to be fluently extended without adding overloads
    ///
    /// e.g.
    /// x => x.WithQueueName("MyQueue")
    /// </summary>
    public interface IRequestConfiguration
    {
        /// <summary>
        /// Sets a priority of the message
        /// </summary>
        /// <param name="priority">The priority to set</param>
        IRequestConfiguration WithPriority(byte priority);

        /// <summary>
        /// Sets an expiration of the request
        /// </summary>
        /// <param name="expiration"></param>
        IRequestConfiguration WithExpiration(TimeSpan expiration);

        /// <summary>
        /// Sets the queue name to publish to
        /// </summary>
        /// <param name="queueName">The queue name</param>
        IRequestConfiguration WithQueueName(string queueName);
    }

    internal class RequestConfiguration : IRequestConfiguration
    {
        public RequestConfiguration(string queueName, TimeSpan expiration)
        {
            QueueName = queueName;
            Expiration = expiration;
        }

        public string QueueName { get; private set; }
        public TimeSpan Expiration { get; private set; }
        public byte? Priority { get; private set; }

        public IRequestConfiguration WithPriority(byte priority)
        {
            Priority = priority;
            return this;
        }

        public IRequestConfiguration WithExpiration(TimeSpan expiration)
        {
            Expiration = expiration;
            return this;
        }

        public IRequestConfiguration WithQueueName(string queueName)
        {
            QueueName = queueName;
            return this;
        }
    }
}
