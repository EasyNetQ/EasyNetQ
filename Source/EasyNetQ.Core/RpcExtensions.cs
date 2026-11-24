using EasyNetQ.Internals;

namespace EasyNetQ;

/// <summary>
///     Various generic extensions for <see cref="IRpc"/>
/// </summary>
public static class RpcExtensions
{
    /// <summary>
    ///     Makes an RPC style request
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="rpc">The rpc instance.</param>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The response</returns>
    public static Task<TResponse> RequestAsync<TRequest, TResponse>(
        this IRpc rpc,
        TRequest request,
        CancellationToken cancellationToken = default
    )
    {
        return rpc.RequestAsync<TRequest, TResponse>(request, _ => { }, cancellationToken);
    }


    /// <summary>
    ///     Set up a responder for an RPC service.
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="rpc">The rpc instance</param>
    /// <param name="responder">A function that performs the response</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task<IAsyncDisposable> RespondAsync<TRequest, TResponse>(
        this IRpc rpc,
        Func<TRequest, TResponse> responder,
        CancellationToken cancellationToken = default
    )
    {
        var asyncResponder = TaskHelpers.FromFunc<TRequest, TResponse>((m, _) => responder(m));
        return rpc.RespondAsync(asyncResponder, _ => { }, cancellationToken);
    }



    /// <summary>
    ///     Set up a responder for an RPC service.
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="rpc">The rpc instance</param>
    /// <param name="responder">A function that performs the response</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task<IAsyncDisposable> RespondAsync<TRequest, TResponse>(
        this IRpc rpc,
        Func<TRequest, Task<TResponse>> responder,
        CancellationToken cancellationToken = default
    ) => rpc.RespondAsync<TRequest, TResponse>((r, _) => responder(r), _ => { }, cancellationToken);

}
