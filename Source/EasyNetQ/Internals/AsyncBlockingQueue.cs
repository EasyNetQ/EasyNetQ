using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public class AsyncBlockingQueue<T>
    {
        private readonly CancellationToken queueCancellation;
        private readonly ConcurrentQueue<T> queue;
        private readonly SemaphoreSlim releaseDequeueEvent;
        private readonly SemaphoreSlim releaseEnqueueEvent;
        
        public AsyncBlockingQueue(int capacity, CancellationToken queueCancellation)
        {
            this.queueCancellation = queueCancellation;
            queue = new ConcurrentQueue<T>();
            releaseDequeueEvent = new SemaphoreSlim(0, capacity);
            releaseEnqueueEvent = new SemaphoreSlim(capacity, capacity);
        }

        public T Dequeue()
        {
            releaseDequeueEvent.Wait(queueCancellation);
            queue.TryDequeue(out var item);
            releaseEnqueueEvent.Release();
            return item;
        }
        
        public async Task EnqueueAsync(T item, CancellationToken cancellation = default(CancellationToken))
        {
            using (var enqueueCts = CancellationTokenSource.CreateLinkedTokenSource(queueCancellation, cancellation))
            {
                await releaseEnqueueEvent.WaitAsync(enqueueCts.Token).ConfigureAwait(false);
                queue.Enqueue(item);
                releaseDequeueEvent.Release();
            }
        }
    }
}