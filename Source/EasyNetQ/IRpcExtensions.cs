namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IRpc"/>
/// </summary>
public static partial class IRpcExtensions
{
    /// <summary>
    ///     Set up a responder for an RPC service.
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="rpc">The RPC</param>
    /// <param name="responder">A function that performs the response</param>
    /// <param name="configure">A function that performs the configuration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task RespondAsync<TRequest, TResponse>(
        this IRpc rpc,
        Func<TRequest, CancellationToken, Task<TResponse>> responder,
        Action<IResponderConfiguration> configure,
        CancellationToken cancellationToken = default
    ) => rpc.RespondAsync<TRequest, TResponse>((request, _, token) => responder(request, token), configure, cancellationToken);
}
