using System;
using System.Threading.Tasks;

namespace EasyNetQ
{
    public static class TaskHelpers
    {
        static TaskHelpers()
        {
            var tcs = new TaskCompletionSource<NullStruct>();
            tcs.SetResult(new NullStruct());
            Completed = tcs.Task;
        }

        public static Task Completed { get; private set; }

        public static Task ExecuteSynchronously(Action action)
        {
            var tcs = new TaskCompletionSource<NullStruct>();
            try
            {
                action();
                tcs.SetResult(new NullStruct());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        public static Task FromException(Exception ex)
        {
            var tcs = new TaskCompletionSource<NullStruct>();
            tcs.SetException(ex);

            return tcs.Task;
        }

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        public static Task<T> FromException<T>(Exception exception)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        private struct NullStruct
        {
        }
    }
}