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
    private readonly Task<Releaser> releaserTask;

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
    public Task<Releaser> AcquireAsync(CancellationToken cancellationToken = default)
    {
        var acquireAsync = semaphore.WaitAsync(cancellationToken);
        return acquireAsync.Status == TaskStatus.RanToCompletion
            ? releaserTask
            : WaitForAcquireAsync(acquireAsync);
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

    /// <summary>
    /// Tries to acquire a lock
    /// </summary>
    /// <param name="result">Releaser, which should be disposed to release a lock</param>
    /// <returns>True if acquired</returns>
    public bool TryAcquire(out Releaser result)
    {
        if (semaphore.Wait(0))
        {
            result = releaser;
            return true;
        }

        result = default;
        return false;
    }

    public readonly struct Releaser : IDisposable
    {
        private readonly SemaphoreSlim? semaphore;

        public Releaser(SemaphoreSlim? semaphore) => this.semaphore = semaphore;

        public void Dispose() => semaphore?.Release();
    }

    /// <inheritdoc />
    public void Dispose() => semaphore.Dispose();

    private async Task<Releaser> WaitForAcquireAsync(Task acquireAsync)
    {
        await acquireAsync.ConfigureAwait(false);
        return releaser;
    }
}
