using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     An RPC style request-response pattern
    /// </summary>
    public interface IRpc : IDisposable
    {
        /// <summary>
        ///     Make a request to an RPC service
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="request">The request message</param>
        /// <param name="configure">
        ///     Fluent configuration e.g. x => x.WithQueueName("uk.london")
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            Action<IRequestConfiguration> configure,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        ///     Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">A function that performs the response</param>
        /// <param name="configure">A function that performs the configuration</param>
        /// <param name="cancellationToken">The cancellation token</param>
        AwaitableDisposable<IDisposable> RespondAsync<TRequest, TResponse>(
            Func<TRequest, CancellationToken, Task<TResponse>> responder,
            Action<IResponderConfiguration> configure,
            CancellationToken cancellationToken = default
        );
    }
}
