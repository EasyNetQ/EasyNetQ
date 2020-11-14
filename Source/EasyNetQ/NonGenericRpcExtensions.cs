using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ
{
    using NonGenericRequestDelegate = Func<IRpc, object, Type, Type, Action<IRequestConfiguration>, CancellationToken, Task<object>>;

    /// <summary>
    ///     Various extensions for IRpc
    /// </summary>
    public static class NonGenericRpcExtensions
    {
        private static readonly ConcurrentDictionary<Tuple<Type, Type>, NonGenericRequestDelegate> RequestDelegates
            = new ConcurrentDictionary<Tuple<Type, Type>, NonGenericRequestDelegate>();

        /// <summary>
        ///     Makes an RPC style request
        /// </summary>
        /// <param name="rpc">The rpc instance.</param>
        /// <param name="request">The request message.</param>
        /// <param name="requestType">The request type</param>
        /// <param name="responseType">The response type</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response</returns>
        public static Task<object> RequestAsync(
            this IRpc rpc,
            object request,
            Type requestType,
            Type responseType,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(rpc, "rpc");

            return rpc.RequestAsync(request, requestType, responseType, c => { }, cancellationToken);
        }

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
        public static Task<object> RequestAsync(
            this IRpc rpc,
            object request,
            Type requestType,
            Type responseType,
            Action<IRequestConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(rpc, "rpc");

            var requestDelegate = RequestDelegates.GetOrAdd(Tuple.Create(requestType, responseType), t =>
            {
                var requestMethodInfo = typeof(IRpc).GetMethod("RequestAsync");
                if (requestMethodInfo == null)
                    throw new MissingMethodException(nameof(IRpc), "RequestAsync");

                var toTaskOfObjectMethodInfo = typeof(NonGenericRpcExtensions).GetMethod(nameof(ToTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static);
                if (toTaskOfObjectMethodInfo == null)
                    throw new MissingMethodException(nameof(NonGenericRpcExtensions), nameof(ToTaskOfObject));

                var genericRequestPublishMethodInfo = requestMethodInfo.MakeGenericMethod(t.Item1, t.Item2);
                var genericToTaskOfObjectMethodInfo = toTaskOfObjectMethodInfo.MakeGenericMethod(t.Item2);
                var rpcParameter = Expression.Parameter(typeof(IRpc), "rpc");
                var requestParameter = Expression.Parameter(typeof(object), "request");
                var requestTypeParameter = Expression.Parameter(typeof(Type), "requestType");
                var responseTypeParameter = Expression.Parameter(typeof(Type), "responseType");
                var configureParameter = Expression.Parameter(typeof(Action<IRequestConfiguration>), "configure");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                var genericRequestMethodCallExpression = Expression.Call(
                    rpcParameter,
                    genericRequestPublishMethodInfo,
                    Expression.Convert(requestParameter, t.Item1),
                    configureParameter,
                    cancellationTokenParameter
                );
                var lambda = Expression.Lambda<NonGenericRequestDelegate>(
                    Expression.Call(null, genericToTaskOfObjectMethodInfo, genericRequestMethodCallExpression),
                    rpcParameter,
                    requestParameter,
                    requestTypeParameter,
                    responseTypeParameter,
                    configureParameter,
                    cancellationTokenParameter
                );
                return lambda.Compile();
            });
            return requestDelegate(rpc, request, requestType, responseType, configure, cancellationToken);
        }

        /// <summary>
        ///     Makes an RPC style request
        /// </summary>
        /// <param name="rpc">The rpc instance.</param>
        /// <param name="request">The request message.</param>
        /// <param name="requestType">The request type</param>
        /// <param name="responseType">The response type</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response</returns>
        public static object Request(
            this IRpc rpc,
            object request,
            Type requestType,
            Type responseType,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(rpc, "rpc");

            return rpc.RequestAsync(request, requestType, responseType, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        ///     Makes an RPC style request
        /// </summary>
        /// <param name="rpc">The rpc instance</param>
        /// <param name="request">The request message</param>
        /// <param name="requestType">The request type</param>
        /// <param name="responseType">The response type</param>
        /// <param name="configure">
        ///     Fluent configuration e.g. x => x.WithQueueName("uk.london")
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response</returns>
        public static object Request(
            this IRpc rpc,
            object request,
            Type requestType,
            Type responseType,
            Action<IRequestConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(rpc, "rpc");

            return rpc.RequestAsync(request, requestType, responseType, configure, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        private static async Task<object> ToTaskOfObject<T>(Task<T> task) => await task.ConfigureAwait(false);
    }
}
