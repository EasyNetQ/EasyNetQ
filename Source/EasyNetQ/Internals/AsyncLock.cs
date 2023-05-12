namespace EasyNetQ.Internals;

/// <summary>
///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
///     the same compatibility as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new EasyNetQ release.
/// </summary>
public readonly struct AsyncLock : IDisposable
{
    private readonly SemaphoreSlim semaphore;
    private readonly Releaser releaser;

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
    }

    /// <summary>
    /// Acquires a lock
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Releaser, which should be disposed to release a lock</returns>
    public ValueTask<Releaser> AcquireAsync(CancellationToken cancellationToken = default)
    {
        var acquireAsync = semaphore.WaitAsync(cancellationToken);
        return acquireAsync.Status == TaskStatus.RanToCompletion
            ? new ValueTask<Releaser>(releaser)
            : WaitForAcquireAsync(acquireAsync);
    }


    /// <summary>
    /// Acquires a lock
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Releaser, which should be disposed to release a lock</returns>
    public ValueTask<Releaser> AcquireAsync(TimeBudget timeout, CancellationToken cancellationToken = default)
    {
        var acquireTask = semaphore.WaitAsync(timeout.Remaining, cancellationToken);
        return acquireTask.Status == TaskStatus.RanToCompletion
            ? acquireTask.GetAwaiter().GetResult()
                ? new ValueTask<Releaser>(releaser)
                : new ValueTask<Releaser>(Task.FromException<Releaser>(new TimeoutException(("The operation has timed out"))))
            : WaitForAcquireAsync(acquireTask);
    }


    /// <summary>
    /// Acquires a lock
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Releaser, which should be disposed to release a lock</returns>
    public Releaser Acquire(CancellationToken cancellationToken = default)
    {
        semaphore.Wait(cancellationToken);
        return releaser;
    }

    public readonly struct Releaser : IDisposable
    {
        private readonly SemaphoreSlim? semaphore;

        public Releaser(SemaphoreSlim? semaphore) => this.semaphore = semaphore;

        public void Dispose() => semaphore?.Release();
    }

    /// <inheritdoc />
    public void Dispose() => semaphore.Dispose();

    private async ValueTask<Releaser> WaitForAcquireAsync(Task acquireTask)
    {
        await acquireTask.ConfigureAwait(false);
        return releaser;
    }

    private async ValueTask<Releaser> WaitForAcquireAsync(Task<bool> acquireTask)
    {
        var acquired = await acquireTask.ConfigureAwait(false);
        return acquired ? releaser : throw new TimeoutException("The operation has timed out");
    }
}
