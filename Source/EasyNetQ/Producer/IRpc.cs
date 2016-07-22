using System;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// An RPC style request-response pattern
    /// </summary>
    public interface IRpc
    {
        /// <summary>
        /// Make a request to an RPC service
        /// </summary>
        /// <param name="endpoint">The endpoint queue</param>
        /// <param name="request">The request message</param>
        /// <param name="timeout">timeout</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> Request<TResponse>(string endpoint, object request, TimeSpan timeout)
            where TResponse : class;

        /// <summary>
        /// Make a request to an RPC service
        /// </summary>
        /// <param name="endpoint">The endpoint queue</param>
        /// <param name="request">The request message</param>
        /// <param name="timeout">timeout</param>
        /// <param name="topic">optional topic</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> Request<TResponse>(string endpoint, object request, TimeSpan timeout, string topic)
            where TResponse : class;

        /// <summary>
        /// Make a request to an RPC service
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="request">The request message</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">A function that performs the response</param>
        IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="endpoint">the service endpoint</param>
        /// <param name="responder">A function that performs the response</param>
        /// <param name="configure">A function that performs the configuration</param>
        IDisposable Respond<TRequest, TResponse>(string endpoint, Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure = null)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="endpoint">the service endpoint</param>
        /// <param name="responder">A function that performs the response</param>
        /// <param name="subscriptionId">optional subscriptionId</param>
        /// <param name="configure">A function that performs the configuration</param>
        IDisposable Respond<TRequest, TResponse>(string endpoint, Func<TRequest, Task<TResponse>> responder, string subscriptionId, Action<ISubscriptionConfiguration> configure)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">A function that performs the response</param>
        /// <param name="configure">A function that performs the configuration</param>
        IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, Action<IResponderConfiguration> configure)
            where TRequest : class
            where TResponse : class;
    }
}