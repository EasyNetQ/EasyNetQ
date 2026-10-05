using EasyNetQ.Hosting;
using EasyNetQ.Internals;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.AutoSubscribe;

/// <summary>
/// Subscribes the generated <see cref="AutoSubscriberConsumer"/>s for the lifetime of the host. Like the fluent
/// consumer host it starts in the background and retries each consumer with backoff (<see cref="ConsumerHostOptions"/>),
/// and reports through <see cref="IConsumerHostStatus"/>.
/// </summary>
internal sealed class AutoSubscriberHostedService : IHostedService, IAsyncDisposable
{
    private readonly AutoSubscriber autoSubscriber;
    private readonly IReadOnlyList<AutoSubscriberConsumer> consumers;
    private readonly ConsumerHostOptions options;
    private readonly ConsumerHostStatus status;
    private readonly ILogger<AutoSubscriberHostedService> logger;
    private readonly List<IAsyncDisposable> subscriptions = new();
    private readonly CancellationTokenSource stopping = new();
    private Task? startup;

    public AutoSubscriberHostedService(
        IBus bus,
        IServiceProvider services,
        IEnumerable<IAutoSubscriberConsumerSource> sources,
        AutoSubscribeOptions autoSubscribeOptions,
        ConsumerHostOptions options,
        ConsumerHostStatus status,
        ILogger<AutoSubscriberHostedService> logger
    )
    {
        autoSubscriber = new AutoSubscriber(bus, services, autoSubscribeOptions.SubscriptionIdPrefix);
        autoSubscribeOptions.Configure?.Invoke(autoSubscriber);
        consumers = sources.SelectMany(s => s.Consumers).Where(c => autoSubscribeOptions.Filter?.Invoke(c.Info) ?? true).ToList();
        this.options = options;
        this.status = status;
        this.logger = logger;
        status.Join(this);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var pending = consumers.ToList();
        status.Pending(this, pending.Count);
        if (options.WaitForStartup)
            return SubscribeAsync(pending, cancellationToken);

        startup = Task.Run(() => SubscribeAsync(pending, stopping.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        stopping.Cancel();
        if (startup is not null)
        {
            try
            {
                await startup.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
        await ReleaseAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        stopping.Cancel();
        await ReleaseAsync().ConfigureAwait(false);
        stopping.Dispose();
    }

    private async ValueTask ReleaseAsync()
    {
        List<IAsyncDisposable> released;
        lock (subscriptions)
        {
            released = subscriptions.AsEnumerable().Reverse().ToList();
            subscriptions.Clear();
        }
        foreach (var subscription in released)
            await subscription.DisposeAsync().ConfigureAwait(false);
    }

    private async Task SubscribeAsync(List<AutoSubscriberConsumer> pending, CancellationToken cancellationToken)
    {
        var delay = options.RetryDelay;
        while (true)
        {
            for (var i = 0; i < pending.Count;)
            {
                try
                {
                    var subscription = await autoSubscriber.SubscribeAsync([pending[i]], cancellationToken).ConfigureAwait(false);
                    lock (subscriptions) subscriptions.Add(subscription);
                    pending.RemoveAt(i);
                    status.Pending(this, pending.Count);
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    status.Failed(exception);
                    logger.AutoSubscribeFailed(exception, pending[i].Info.ConcreteType.Name, pending[i].Info.MessageType.Name, delay);
                    i++;
                }
            }

            if (pending.Count == 0)
            {
                status.Started(this);
                logger.AutoSubscribed(consumers.Count);
                return;
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, options.MaxRetryDelay.Ticks));
        }
    }
}
