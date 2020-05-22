using System;
using System.Collections.Generic;
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
    public sealed class AsyncQueue<T> : IDisposable
    {
        private readonly object mutex = new object();
        private readonly Queue<T> elements = new Queue<T>();
        private readonly Queue<TaskCompletionSource<T>> waiters = new Queue<TaskCompletionSource<T>>();

        public AsyncQueue(IEnumerable<T> collection)
        {
            foreach(var element in collection)
                elements.Enqueue(element);
        }

        public Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            lock (mutex)
            {
                if (elements.Count > 0)
                    return Task.FromResult(elements.Dequeue());

                FreeCancelledWaiters();

                var waiter = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                waiter.AttachCancellation(cancellationToken);
                waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }

        public void Enqueue(T element)
        {
            lock (mutex)
            {
                var wasAttachedToWaiter = false;
                while (waiters.Count > 0)
                {
                    var waiter = waiters.Dequeue();
                    if (waiter.TrySetResult(element))
                        wasAttachedToWaiter = true;
                }

                FreeCancelledWaiters();

                if (wasAttachedToWaiter)
                    return;

                elements.Enqueue(element);
            }
        }

        private void FreeCancelledWaiters()
        {
            while (waiters.Count > 0)
            {
                var waiter = waiters.Peek();
                if (waiter.Task.IsCompleted)
                    waiters.Dequeue();
            }
        }

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
    }
}
