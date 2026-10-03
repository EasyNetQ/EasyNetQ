namespace EasyNetQ.Hosting;

/// <summary>
///     Startup state of the fluent-registered consumers, for readiness checks and tests
/// </summary>
public interface IConsumerHostStatus
{
    /// <summary>Every registered consumer is consuming</summary>
    bool IsStarted { get; }

    /// <summary>Consumers still waiting to start</summary>
    int PendingConsumers { get; }

    /// <summary>The last startup failure, cleared once every consumer runs</summary>
    Exception? LastError { get; }

    /// <summary>Completes when every registered consumer is consuming</summary>
    Task WaitForStartedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     The <see cref="IConsumerHostStatus" /> the consumer host updates
/// </summary>
public sealed class ConsumerHostStatus : IConsumerHostStatus
{
    private readonly TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile int pending;
    private volatile Exception? lastError;

    /// <inheritdoc />
    public bool IsStarted => started.Task.IsCompleted;

    /// <inheritdoc />
    public int PendingConsumers => pending;

    /// <inheritdoc />
    public Exception? LastError => lastError;

    /// <inheritdoc />
    public Task WaitForStartedAsync(CancellationToken cancellationToken = default)
        => started.Task.IsCompleted || !cancellationToken.CanBeCanceled
            ? started.Task
            : WaitAsync(cancellationToken);

    private async Task WaitAsync(CancellationToken cancellationToken)
    {
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(), cancelled);
        await (await Task.WhenAny(started.Task, cancelled.Task).ConfigureAwait(false)).ConfigureAwait(false);
    }

    internal void Pending(int count) => pending = count;

    internal void Failed(Exception exception) => lastError = exception;

    internal void Started()
    {
        pending = 0;
        lastError = null;
        started.TrySetResult(true);
    }
}
