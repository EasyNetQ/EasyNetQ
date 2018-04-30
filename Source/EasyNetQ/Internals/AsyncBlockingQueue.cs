using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public class AsyncBlockingQueue<T>
    {
        private readonly ConcurrentQueue<T> queue;
        private readonly SemaphoreSlim releaseDequeueEvent;
        private readonly SemaphoreSlim releaseEnqueueEvent;
        
        public AsyncBlockingQueue(int capacity)
        {
            queue = new ConcurrentQueue<T>();
            releaseDequeueEvent = new SemaphoreSlim(0, capacity);
            releaseEnqueueEvent = new SemaphoreSlim(capacity, capacity);
        }

        public T Dequeue(CancellationToken cancellation = default(CancellationToken))
        {
            releaseDequeueEvent.Wait(cancellation);
            queue.TryDequeue(out var item);
            releaseEnqueueEvent.Release();
            return item;
        }

        public bool TryDequeue(out T item)
        {
            if (releaseDequeueEvent.Wait(0) && queue.TryDequeue(out item))
            {
                releaseEnqueueEvent.Release();
                return true;
            }

            item = default(T);
            return false;
        }

        public async Task EnqueueAsync(T item, CancellationToken cancellation = default(CancellationToken))
        {
            await releaseEnqueueEvent.WaitAsync(cancellation).ConfigureAwait(false);
            queue.Enqueue(item); 
            releaseDequeueEvent.Release();
        }
    }
}