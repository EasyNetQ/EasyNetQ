using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim semaphore;
        private readonly IDisposable semaphoreReleaser;

        public AsyncLock()
        {
            semaphore = new SemaphoreSlim(1);
            semaphoreReleaser = new SemaphoreSlimReleaser(semaphore);
        }

        public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return semaphoreReleaser;
        }

        private class SemaphoreSlimReleaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            public SemaphoreSlimReleaser(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            public void Dispose()
            {
                semaphore.Release();
            }
        }
    }
}
