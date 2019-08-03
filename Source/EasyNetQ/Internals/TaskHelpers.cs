using System;
using System.Threading;
#if NETFX
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
#endif
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public static class TaskHelpers
    {
#if NETFX && !NET46
        /// <summary>
        ///     We want to prevent callers hijacking the reader thread; this is a bit nasty, but works;
        ///     see http://stackoverflow.com/a/22588431/23354 for more information; a huge
        ///     thanks to Eli Arbel for spotting this (even though it is pure evil; it is *my kind of evil*)
        /// </summary>
        private static readonly Func<Task, bool> IsSyncSafe;

        static TaskHelpers()
        {
            try
            {
                var taskType = typeof(Task);
                var continuationField = taskType.GetField("m_continuationObject", BindingFlags.Instance | BindingFlags.NonPublic);
                var safeScenario = taskType.GetNestedType("SetOnInvokeMres", BindingFlags.NonPublic);
                if (continuationField != null && continuationField.FieldType == typeof(object) && safeScenario != null)
                {
                    var method = new DynamicMethod("IsSyncSafe", typeof(bool), new[] { typeof(Task) }, typeof(Task), true);
                    var il = method.GetILGenerator();
                    //var hasContinuation = il.DefineLabel();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, continuationField);
                    Label nonNull = il.DefineLabel(), goodReturn = il.DefineLabel();
                    // check if null
                    il.Emit(OpCodes.Brtrue_S, nonNull);
                    il.MarkLabel(goodReturn);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Ret);

                    // check if is a SetOnInvokeMres - if so, we're OK
                    il.MarkLabel(nonNull);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, continuationField);
                    il.Emit(OpCodes.Isinst, safeScenario);
                    il.Emit(OpCodes.Brtrue_S, goodReturn);

                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ret);

                    IsSyncSafe = (Func<Task, bool>)method.CreateDelegate(typeof(Func<Task, bool>));

                    // and test them (check for an exception etc)
                    var tcs = new TaskCompletionSource<int>();
                    var expectTrue = IsSyncSafe(tcs.Task);
                    tcs.Task.ContinueWith(delegate { });
                    var expectFalse = IsSyncSafe(tcs.Task);
                    tcs.SetResult(0);
                    if (!expectTrue || expectFalse)
                    {
                        Debug.WriteLine("IsSyncSafe reported incorrectly!");
                        Trace.WriteLine("IsSyncSafe reported incorrectly!");
                        // revert to not trusting /them
                        IsSyncSafe = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Trace.WriteLine(ex.Message);
                IsSyncSafe = null;
            }
            if (IsSyncSafe == null)
                IsSyncSafe = t => false; // assume: not
        }
#endif
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

        public static TaskCompletionSource<T> CreateTcs<T>()
        {
#if NETFX && !NET46
            return new TaskCompletionSource<T>();
#else
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        }

        public static void TrySetResultAsynchronously<T>(this TaskCompletionSource<T> source, T result)
        {
#if NETFX && !NET46
            if (IsSyncSafe(source.Task))
#else
            if ((source.Task.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously) == TaskCreationOptions.RunContinuationsAsynchronously)
#endif
            {
                source.TrySetResult(result);
            }
            else
            {
                Task.Run(() => source.TrySetResult(result));
            }
        }

        public static void TrySetCanceledAsynchronously<T>(this TaskCompletionSource<T> source)
        {
#if NETFX && !NET46
            if (IsSyncSafe(source.Task))
#else
            if ((source.Task.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously) == TaskCreationOptions.RunContinuationsAsynchronously)
#endif
            {
                source.TrySetCanceled();
            }
            else
            {
                Task.Run(() => source.TrySetCanceled());
            }
        }

        public static void TrySetExceptionAsynchronously<T>(this TaskCompletionSource<T> source, Exception exception)
        {
#if NETFX && !NET46
            if (IsSyncSafe(source.Task))
#else
            if ((source.Task.CreationOptions & TaskCreationOptions.RunContinuationsAsynchronously) == TaskCreationOptions.RunContinuationsAsynchronously)
#endif
            {
                source.TrySetException(exception);
            }
            else
            {
                Task.Run(() => source.TrySetException(exception));
            }
        }
    }
}