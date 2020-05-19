using System;

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
        /// Sets an expiration of the request
        /// </summary>
        /// <param name="expiration"></param>
        /// <returns></returns>
        IRequestConfiguration WithExpiration(TimeSpan expiration);

        /// <summary>
        /// Sets the queue name to publish to
        /// </summary>
        /// <param name="queueName">The queue name</param>
        /// <returns>IPublishConfiguration</returns>
        IRequestConfiguration WithQueueName(string queueName);
    }

    /// <inheritdoc />
    public class RequestConfiguration : IRequestConfiguration
    {
        public RequestConfiguration(string queueName, TimeSpan expiration)
        {
            QueueName = queueName;
            Expiration = expiration;
        }

        public string QueueName { get; private set; }
        public TimeSpan Expiration { get; private set; }

        /// <inheritdoc />
        public IRequestConfiguration WithExpiration(TimeSpan expiration)
        {
            Expiration = expiration;
            return this;
        }

        /// <inheritdoc />
        public IRequestConfiguration WithQueueName(string queueName)
        {
            QueueName = queueName;
            return this;
        }
    }
}
