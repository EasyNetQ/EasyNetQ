using System;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public static class TaskHelpers
    {
        public static Task ExecuteSynchronously(Action action)
        {
            try
            {
                action();
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }
    }
}
