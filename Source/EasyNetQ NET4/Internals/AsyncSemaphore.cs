using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     AsyncSemaphore should be used with a lot of care.
    /// </summary>
    public class AsyncSemaphore
    {
        private readonly object locker = new object();
        private readonly Queue<TaskCompletionSource<object>> waiters = new Queue<TaskCompletionSource<object>>();
        private volatile int available;

        public AsyncSemaphore(int initial)
        {
            available = initial;
        }

        public int Available
        {
            get { return available; }
        }

        public void Wait()
        {
            TaskCompletionSource<object> waiter;
            lock (locker)
            {
                if (available > 0)
                {
                    --available;
                    return;
                }
                waiter = new TaskCompletionSource<object>();
                waiters.Enqueue(waiter);
            }
            waiter.Task.Wait();
        }

        public Task WaitAsync()
        {
            TaskCompletionSource<object> waiter;
            lock (locker)
            {
                if (available > 0)
                {
                    --available;
                    return TaskHelpers.Completed;
                }

                waiter = new TaskCompletionSource<object>();
                waiters.Enqueue(waiter);
            }
            return waiter.Task;
        }

        public void Release()
        {
            TaskCompletionSource<object> waiter;
            lock (locker)
            {
                if (waiters.Count == 0)
                {
                    ++available;
                    return;
                }
                waiter = waiters.Dequeue();
            }
            waiter.TrySetResultSafe(null);
        }
    }
}