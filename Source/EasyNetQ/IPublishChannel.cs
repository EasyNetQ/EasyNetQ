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

        /// <summary>
        /// Makes an RPC style asynchronous request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="onResponse">The action to run when the response is received.</param>
        void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse);

        /// <summary>
        /// The bus that created this channel
        /// </summary>
        IBus Bus { get; }
    }
}