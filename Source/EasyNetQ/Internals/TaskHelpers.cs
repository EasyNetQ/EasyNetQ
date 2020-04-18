using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public static class TaskHelpers
    {
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

        public static Func<T1, T2, T3, CancellationToken, Task> FromAction<T1, T2, T3>(Action<T1, T2, T3, CancellationToken> action)
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

        public static Task FromCancelled()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        public static Task<T> FromCancelled<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        public static Task FromException(Exception exception)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        public static Task<T> FromException<T>(Exception exception)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        public static Task<T> FromResult<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static Task Completed { get; } = FromResult<object>(null);
    }
}
