using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     AsyncSemaphore should be used with a lot of care.
    /// </summary>
    public class AsyncLock
    {
        private readonly SemaphoreSlim semaphore;
        private readonly SemaphoreSlimReleaser semaphoreReleaser;

        public AsyncLock()
        {
            semaphore = new SemaphoreSlim(1);
            semaphoreReleaser = new SemaphoreSlimReleaser(semaphore);
        }

        public IDisposable Acquire()
        {
            semaphore.Wait();
            return semaphoreReleaser;
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