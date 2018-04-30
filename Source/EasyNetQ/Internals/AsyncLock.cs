using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public class AsyncLock
    {
        private readonly SemaphoreSlim semaphore;
        private readonly SemaphoreSlimReleaser semaphoreReleaser;

        public AsyncLock()
        {
            semaphore = new SemaphoreSlim(1);
            semaphoreReleaser = new SemaphoreSlimReleaser(semaphore);
        }

        public async Task<IDisposable> AcquireAsync()
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
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