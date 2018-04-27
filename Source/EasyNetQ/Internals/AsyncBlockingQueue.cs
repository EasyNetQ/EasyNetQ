using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public class AsyncBlockingQueue<T> : IDisposable
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

        public async Task EnqueueAsync(T item, CancellationToken cancellation = default(CancellationToken))
        {
            await releaseEnqueueEvent.WaitAsync(cancellation);
            queue.Enqueue(item); 
            releaseDequeueEvent.Release();
        }

        public void Dispose()
        {
            releaseEnqueueEvent.Dispose();
            releaseDequeueEvent.Dispose();
        }
    }
}