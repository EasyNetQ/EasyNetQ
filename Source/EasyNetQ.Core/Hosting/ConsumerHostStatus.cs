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
///     The <see cref="IConsumerHostStatus" /> the consumer hosts update: the fluent consumer host and, when
///     registered, the auto-subscriber host. It reports started once every host that joined has started.
/// </summary>
public sealed class ConsumerHostStatus : IConsumerHostStatus
{
    private readonly TaskCompletionSource<bool> started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Dictionary<object, int> pendingByHost = new();
    private readonly HashSet<object> startedHosts = new();
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

    // hosts join from their constructors: the generic host resolves every IHostedService before starting any
    internal void Join(object host)
    {
        lock (pendingByHost)
        {
            if (!pendingByHost.ContainsKey(host))
                pendingByHost[host] = 0;
        }
    }

    internal void Pending(object host, int count)
    {
        lock (pendingByHost)
        {
            pendingByHost[host] = count;
            pending = pendingByHost.Values.Sum();
        }
    }

    internal void Failed(Exception exception) => lastError = exception;

    internal void Started(object host)
    {
        lock (pendingByHost)
        {
            pendingByHost[host] = 0;
            startedHosts.Add(host);
            pending = pendingByHost.Values.Sum();
            if (startedHosts.Count < pendingByHost.Count)
                return;
        }
        lastError = null;
        started.TrySetResult(true);
    }
}
