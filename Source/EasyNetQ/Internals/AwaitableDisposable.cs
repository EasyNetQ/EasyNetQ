using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public struct AwaitableDisposable<T> where T : IDisposable
    {
        private readonly Task<T> task;

        public AwaitableDisposable(Task<T> task)
        {
            this.task = task ?? throw new ArgumentNullException(nameof(task));
        }

        public Task<T> AsTask()
        {
            return task;
        }

        public static implicit operator Task<T>(AwaitableDisposable<T> source)
        {
            return source.AsTask();
        }

        public TaskAwaiter<T> GetAwaiter()
        {
            return task.GetAwaiter();
        }

        public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext)
        {
            return task.ConfigureAwait(continueOnCapturedContext);
        }
    }
}