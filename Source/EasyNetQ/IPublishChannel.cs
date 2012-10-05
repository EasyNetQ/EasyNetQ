using System;
using System.Collections.Generic;
using EasyNetQ.FluentConfiguration;

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
        /// <param name="message">The message to publish</param>
        /// <param name="configure">Configure the publish e.g. x => x.WithTopic("1010.brighton")</param>
        void Publish<T>(T message, Action<IPublishConfiguration<T>> configure);

        /// <summary>
        /// Makes an RPC style asynchronous request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="onResponse">The action to run when the response is received.</param>
        void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse);

        /// <summary>
        /// Makes an RPC style asynchronous request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="onResponse">The action to run when the response is received.</param>
        /// <param name="arguments">AMQP arguments. For e.q. Message TTL("x-message-ttl", "60"), High Availability policy("x-ha-policy", "all") and so on.</param>
        void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse, IDictionary<string, object> arguments);

        /// <summary>
        /// The bus that created this channel
        /// </summary>
        IBus Bus { get; }
    }
}