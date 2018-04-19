using System;
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
#if NETFX
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
        public static Task ExecuteSynchronously(Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            try
            {
                action();
                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            return tcs.Task;
        }

        public static Task FromException(Exception ex)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(ex);
            return tcs.Task;
        }
        
#if NETFX
        public static void TrySetResultAsynchronously<T>(this TaskCompletionSource<T> source, T result)
        {
            if (IsSyncSafe(source.Task))
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
            if (IsSyncSafe(source.Task))
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
            if (IsSyncSafe(source.Task))
            {
                source.TrySetException(exception);
            }
            else
            {
                Task.Run(() => source.TrySetException(exception));
            }
        }
#endif
    }
}