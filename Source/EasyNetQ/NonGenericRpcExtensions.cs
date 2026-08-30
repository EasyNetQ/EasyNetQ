using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace EasyNetQ;

using NonGenericRequestDelegate = Func<IRpc, object, Action<IRequestConfiguration>, CancellationToken, Task<object>>;

/// <summary>
///     Various non-generic extensions for <see cref="IRpc"/>
/// </summary>
public static class NonGenericRpcExtensions
{
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), NonGenericRequestDelegate> RequestDelegates = new();

    /// <summary>
    ///     Makes an RPC style request
    /// </summary>
    /// <param name="rpc">The rpc instance.</param>
    /// <param name="request">The request message.</param>
    /// <param name="requestType">The request type</param>
    /// <param name="responseType">The response type</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The response</returns>
    [RequiresDynamicCode(NonGenericBridge.RequiresDynamicCodeMessage)]
    public static Task<object> RequestAsync(
        this IRpc rpc,
        object request,
        Type requestType,
        Type responseType,
        CancellationToken cancellationToken = default
    ) => rpc.RequestAsync(request, requestType, responseType, _ => { }, cancellationToken);

    /// <summary>
    ///     Makes an RPC style request
    /// </summary>
    /// <param name="rpc">The rpc instance.</param>
    /// <param name="request">The request message.</param>
    /// <param name="requestType">The request type</param>
    /// <param name="responseType">The response type</param>
    /// <param name="configure">
    ///     Fluent configuration e.g. x => x.WithQueueName("uk.london")
    /// </param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The response</returns>
    [RequiresDynamicCode(NonGenericBridge.RequiresDynamicCodeMessage)]
    public static Task<object> RequestAsync(
        this IRpc rpc,
        object request,
        Type requestType,
        Type responseType,
        Action<IRequestConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        // The request/response pair needs a generic method closed over two runtime types; a static bridge method
        // plus CreateDelegate (cached per pair) replaces the 8.x expression-tree compilation
        var requestDelegate = RequestDelegates.GetOrAdd((requestType, responseType), static key =>
        {
            var bridgeMethod = typeof(NonGenericRpcExtensions).GetMethod(nameof(RequestBridgeAsync), BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(nameof(NonGenericRpcExtensions), nameof(RequestBridgeAsync));
            var closedBridgeMethod = bridgeMethod.MakeGenericMethod(key.RequestType, key.ResponseType);
            return (NonGenericRequestDelegate)closedBridgeMethod.CreateDelegate(typeof(NonGenericRequestDelegate));
        });
        return requestDelegate(rpc, request, configure, cancellationToken);
    }

    private static async Task<object> RequestBridgeAsync<TRequest, TResponse>(
        IRpc rpc, object request, Action<IRequestConfiguration> configure, CancellationToken cancellationToken
    ) => (await rpc.RequestAsync<TRequest, TResponse>((TRequest)request, configure, cancellationToken).ConfigureAwait(false))!;
}
