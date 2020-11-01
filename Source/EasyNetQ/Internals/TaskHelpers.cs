using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public static class TaskHelpers
    {
        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static Func<T1, CancellationToken, Task<T2>> FromFunc<T1, T2>(Func<T1, CancellationToken, T2> func)
        {
            return (x, c) =>
            {
                var tcs = new TaskCompletionSource<T2>();
                try
                {
                    var result = func(x, c);
                    tcs.SetResult(result);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }

                return tcs.Task;
            };
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static Func<T1, T2, T3, CancellationToken, Task> FromAction<T1, T2, T3>(
            Action<T1, T2, T3, CancellationToken> action
        )
        {
            return (x, y, z, c) =>
            {
                var tcs = new TaskCompletionSource<object>();
                try
                {
                    action(x, y, z, c);
                    tcs.SetResult(null);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }

                return tcs.Task;
            };
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static Func<T1, T2, CancellationToken, Task> FromAction<T1, T2>(Action<T1, T2, CancellationToken> action)
        {
            return (x, y, c) =>
            {
                var tcs = new TaskCompletionSource<object>();
                try
                {
                    action(x, y, c);
                    tcs.SetResult(null);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }

                return tcs.Task;
            };
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static Func<T1, CancellationToken, Task> FromAction<T1>(Action<T1, CancellationToken> action)
        {
            return (x, c) =>
            {
                var tcs = new TaskCompletionSource<object>();
                try
                {
                    action(x, c);
                    tcs.SetResult(null);
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }

                return tcs.Task;
            };
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public static void AttachCancellation<T>(
            this TaskCompletionSource<T> taskCompletionSource, CancellationToken cancellationToken
        )
        {
            if (!cancellationToken.CanBeCanceled || taskCompletionSource.Task.IsCompleted)
                return;

            if (cancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetCanceled(cancellationToken);
                return;
            }

            var state = new TcsWithCancellationToken<T>(taskCompletionSource, cancellationToken);
            state.CancellationTokenRegistration = cancellationToken.Register(
                s =>
                {
                    var t = (TcsWithCancellationToken<T>) s;
                    t.Tcs.TrySetCanceled(t.CancellationToken);
                },
                state,
                false
            );
            taskCompletionSource.Task.ContinueWith(
                (_, s) =>
                {
                    var r = (TcsWithCancellationToken<T>) s;
                    r.CancellationTokenRegistration.Dispose();
                },
                state,
                default,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }

        private class TcsWithCancellationToken<T>
        {
            public TcsWithCancellationToken(TaskCompletionSource<T> tcs, CancellationToken cancellationToken)
            {
                Tcs = tcs;
                CancellationToken = cancellationToken;
            }

            public TaskCompletionSource<T> Tcs { get; }
            public CancellationToken CancellationToken { get; }
            public CancellationTokenRegistration CancellationTokenRegistration { get; set; }
        }
    }
}
