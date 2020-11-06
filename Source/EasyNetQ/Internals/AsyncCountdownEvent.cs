using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
    ///     the same compatibility as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EasyNetQ release.
    /// </summary>
    public sealed class AsyncCountdownEvent : IDisposable
    {
        private readonly object mutex = new object();
        private readonly Queue<TaskCompletionSource<bool>> waiters = new Queue<TaskCompletionSource<bool>>();
        private long count;

        /// <inheritdoc />
        public void Dispose()
        {
            lock (mutex)
            {
                while (waiters.Count > 0)
                {
                    var waiter = waiters.Dequeue();
                    waiter.TrySetCanceled();
                }
            }
        }

        /// <summary>
        ///     Increments counter
        /// </summary>
        public void Increment()
        {
            lock (mutex)
            {
                count += 1;
            }
        }

        /// <summary>
        ///     Decrements counter
        /// </summary>
        public void Decrement()
        {
            lock (mutex)
            {
                count -= 1;
                while (count == 0 && waiters.Count > 0)
                {
                    var waiter = waiters.Dequeue();
                    if (waiter.TrySetResult(true))
                        break;
                }
            }
        }

        /// <summary>
        ///     Waits until counter is zero
        /// </summary>
        public void Wait()
        {
            TaskCompletionSource<bool> waiter = null;
            lock (mutex)
            {
                if (count > 0)
                {
                    waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    waiters.Enqueue(waiter);
                }
            }

            waiter?.Task.GetAwaiter().GetResult();
        }
    }
}
