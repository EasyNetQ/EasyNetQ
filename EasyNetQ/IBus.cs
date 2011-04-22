using System;

namespace EasyNetQ
{
    /// <summary>
    /// Provides a simple Publish/Subscribe and Request/Response API for a message bus.
    /// </summary>
    public interface IBus : IDisposable
    {
        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="message">The message to publish</param>
        void Publish<T>(T message);

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives.
        /// </param>
        void Subscribe<T>(string subscriptionId, Action<T> onMessage);

        /// <summary>
        /// Makes a single RPC style asynchronous request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="onResponse">The action to run when the response is received.</param>
        void Request<TRequest, TResponse>(TRequest request, Action<TResponse> onResponse);

        /// <summary>
        /// Creates a requester that can be used to make multiple requests.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="onResponse">The action to run when the response is received.</param>
        /// <returns>An action to make the request.</returns>
        Action<TRequest> Request<TRequest, TResponse>(Action<TResponse> onResponse);

        /// <summary>
        /// Responds to an RPC request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="responder">
        /// A function to run when the request is received. It should return the response.
        /// </param>
        void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder);
    }
}