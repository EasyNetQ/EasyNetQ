using System;
using System.Threading.Tasks;

namespace EasyNetQ
{
    public static class TaskHelpers
    {
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

        private struct NullStruct{}
    
    }
}