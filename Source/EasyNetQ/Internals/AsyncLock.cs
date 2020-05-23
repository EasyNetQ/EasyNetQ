using System;
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
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim semaphore;
        private readonly IDisposable releaser;
        private readonly Task<IDisposable> releaserTask;

        /// <summary>
        ///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
        ///     the same compatibility as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new EasyNetQ release.
        /// </summary>
        public AsyncLock()
        {
            semaphore = new SemaphoreSlim(1);
            releaser = new Releaser(semaphore);
            releaserTask = Task.FromResult(releaser);
        }

        /// <summary>
        /// Acquires a lock
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Releaser, which should be disposed to release a lock</returns>
        public Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
        {
            var acquireTask = semaphore.WaitAsync(cancellationToken);
            return acquireTask.IsCompleted
                ? releaserTask
                : acquireTask.ContinueWith(
                    (_, state) => (IDisposable)state,
                    releaser,
                    default,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                );
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            public Releaser(SemaphoreSlim semaphore) => this.semaphore = semaphore;

            public void Dispose() => semaphore.Release();
        }

        /// <inheritdoc />
        public void Dispose() => semaphore.Dispose();
    }
}
