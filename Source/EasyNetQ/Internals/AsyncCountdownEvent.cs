namespace EasyNetQ.Internals;

/// <summary>
///     This is an internal API that supports the EasyNetQ infrastructure and not subject to
///     the same compatibility as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new EasyNetQ release.
/// </summary>
public sealed class AsyncCountdownEvent : IDisposable
{
    private readonly object mutex = new();
    private readonly Queue<TaskCompletionSource<bool>> waiters = new();
    private long count;

    public AsyncCountdownEvent(long initialCount = 0)
    {
        if (initialCount < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCount), "Initial count cannot be negative");

        count = initialCount;
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
        }
    }

    /// <summary>
    ///     Increments counter
    /// </summary>
    public void Increment()
    {
        lock (mutex)
        {
            count += 1;
        }
    }

    /// <summary>
    ///     Decrements counter
    /// </summary>
    public void Decrement()
    {
        lock (mutex)
        {
            if (count <= 0)
                throw new InvalidOperationException("Counter is already zero");

            count -= 1;
            while (count == 0 && waiters.Count > 0)
            {
                var waiter = waiters.Dequeue();
                if (waiter.TrySetResult(true))
                    break;
            }
        }
    }

    /// <summary>
    ///     Waits until counter is zero
    /// </summary>
    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool> waiter;
        lock (mutex)
        {
            if (count <= 0) return Task.CompletedTask;

            waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            waiters.Enqueue(waiter);
        }

        waiter.AttachCancellation(cancellationToken);
        return waiter.Task;
    }
}
