using System;

namespace EasyNetQ
{
    /// <summary>
    /// Represents a channel for messages publication. It must not be shared between threads and
    /// should be disposed after use.
    /// </summary>
    public interface IPublishChannel : IDisposable
    {
        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        void Publish<T>(T message);

        /// <summary>
        /// Publishes a message with a topic
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="topic">The topic</param>
        /// <param name="message">The message to publish</param>
        void Publish<T>(string topic, T message);
    }
}