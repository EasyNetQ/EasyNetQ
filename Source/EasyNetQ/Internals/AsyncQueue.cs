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
        private readonly Queue<T> elements = new Queue<T>();
        private readonly object mutex = new object();
        private readonly Queue<TaskCompletionSource<T>> waiters = new Queue<TaskCompletionSource<T>>();

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public AsyncQueue()
        {
        }

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public AsyncQueue(IEnumerable<T> collection)
        {
            foreach (var element in collection)
                elements.Enqueue(element);
        }

        /// <summary>
        ///     Returns count of elements in queue
        /// </summary>
        public int Count
        {
            get
            {
                lock (mutex)
                {
                    return elements.Count;
                }
            }
        }

        /// <summary>
        ///     Tries to take the element from queue
        /// </summary>
        /// <param name="element">Dequeued element</param>
        /// <returns>True if an element was dequeued</returns>
        public bool TryDequeue(out T element)
        {
            lock (mutex)
            {
                var hasElements = elements.Count > 0;
                element = hasElements ? elements.Dequeue() : default;
                return hasElements;
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
                elements.Clear();
            }
        }

        /// <summary>
        ///     Takes the element from queue
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The dequeued element</returns>
        public Task<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            lock (mutex)
            {
                if (elements.Count > 0)
                    return Task.FromResult(elements.Dequeue());

                CleanUpCancelledWaiters();

                var waiter = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                waiter.AttachCancellation(cancellationToken);
                waiters.Enqueue(waiter);
                return waiter.Task;
            }
        }

        /// <summary>
        ///     Adds the element to queue
        /// </summary>
        /// <param name="element">The element to enqueue</param>
        public void Enqueue(T element)
        {
            lock (mutex)
            {
                while (waiters.Count > 0)
                {
                    var waiter = waiters.Dequeue();
                    if (waiter.TrySetResult(element))
                    {
                        CleanUpCancelledWaiters();
                        return;
                    }
                }

                elements.Enqueue(element);
            }
        }

        private void CleanUpCancelledWaiters()
        {
            while (waiters.Count > 0)
            {
                var waiter = waiters.Peek();
                if (waiter.Task.IsCompleted)
                    waiters.Dequeue();
                else
                    break;
            }
        }
    }
}
