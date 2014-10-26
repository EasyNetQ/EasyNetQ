﻿using System;
using System.Threading.Tasks;

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
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="request">The request message</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Make a request to an RPC service, to the specified queue.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="queue">The queue to send the message to</param>
        /// <param name="request">The request message</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> Request<TRequest, TResponse>(string queue, TRequest request)
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
        /// <param name="queue">The queue to receive from</param>
        /// <param name="responder">A function that performs the response</param>
        IDisposable Respond<TRequest, TResponse>(string queue, Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class;
    }
}