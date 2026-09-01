using System.Diagnostics;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer;

/// <summary>
///     Represent an abstract consumer
/// </summary>
public interface IConsumer : IAsyncDisposable
{
    /// <summary>
    ///     Unique consumer id
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     Starts the consumer asynchronously
    /// </summary>
    /// <returns>Disposable to stop the consumer</returns>
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Configuration of the consumer for a queue
/// </summary>
public class PerQueueConsumerConfiguration
{
    /// <summary>
    ///     Creates PerQueueConsumerConfiguration
    /// </summary>
    public PerQueueConsumerConfiguration(
        bool autoAck,
        string consumerTag,
        bool isExclusive,
        IDictionary<string, object> arguments,
        ConsumerContext context
    )
    {
        AutoAck = autoAck;
        ConsumerTag = consumerTag;
        IsExclusive = isExclusive;
        Arguments = arguments;
        Context = context;
    }

    /// <summary>
    ///     Indicates whether a consumer auto-acks messages
    /// </summary>
    public bool AutoAck { get; }

    /// <summary>
    ///     Tag of a consumer
    /// </summary>
    public string ConsumerTag { get; }

    /// <summary>
    ///     Indicates whether a consumer is exclusive
    /// </summary>
    public bool IsExclusive { get; }

    /// <summary>
    ///     Custom arguments
    /// </summary>
    public IDictionary<string, object> Arguments { get; }

    /// <summary>
    ///     Consumer-layer context: owns the message pipeline and the pooled message contexts
    /// </summary>
    public ConsumerContext Context { get; }
}

/// <summary>
///     Configuration of the consumer
/// </summary>
public class ConsumerConfiguration
{
    /// <summary>
    ///     Creates ConsumerConfiguration
    /// </summary>
    public ConsumerConfiguration(
        ushort prefetchCount,
        IReadOnlyDictionary<Queue, PerQueueConsumerConfiguration> perQueueConfigurations
    )
    {
        PrefetchCount = prefetchCount;
        PerQueueConfigurations = perQueueConfigurations;
    }

    /// <summary>
    ///     PrefetchCount for the consumer
    /// </summary>
    public ushort PrefetchCount { get; }

    /// <summary>
    ///     Configurations of the consumer for queues
    /// </summary>
    public IReadOnlyDictionary<Queue, PerQueueConsumerConfiguration> PerQueueConfigurations { get; }
}

/// <inheritdoc />
public sealed class Consumer : IConsumer
{
    // Safety net only: restarts are event-driven (connection recovered + channel faulted), the timer just
    // catches anything those events miss
    private static readonly TimeSpan RestartConsumingPeriod = TimeSpan.FromSeconds(60);

    // A channel-fault restart runs immediately; if faults repeat (e.g. consuming from a deleted queue keeps
    // soft-erroring the fresh channel) further event-driven attempts are suppressed for this long and the
    // safety-net timer paces the retries instead of a hot restart -> soft error -> restart loop
    private static readonly TimeSpan MinChannelFaultRestartInterval = TimeSpan.FromSeconds(5);

    private readonly ConsumerConfiguration configuration;
    private readonly IEventBus eventBus;
    private readonly IInternalConsumerFactory internalConsumerFactory;
    private readonly IDisposable[] disposables;
    private readonly object mutex = new();

    private volatile IInternalConsumer consumer;
    private volatile bool disposed;
    private long lastChannelFaultRestartTimestamp;
    private readonly ILogger<Consumer> logger;
    /// <summary>
    ///     Creates Consumer
    /// </summary>
    public Consumer(
        ILogger<Consumer> logger,
        ConsumerConfiguration configuration,
        IInternalConsumerFactory internalConsumerFactory,
        IEventBus eventBus
    )
    {
        this.logger = logger;
        this.configuration = configuration;
        this.internalConsumerFactory = internalConsumerFactory;
        this.eventBus = eventBus;
        disposables =
        [
            eventBus.Subscribe<ConnectionRecoveredEvent>(OnConnectionRecovered),
            eventBus.Subscribe<ConnectionDisconnectedEvent>(OnConnectionDisconnected),
            Timers.Start(RestartConsumingPeriodically, RestartConsumingPeriod, logger)
        ];
    }

    /// <inheritdoc />
    public Guid Id { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(Consumer));

        lock (mutex)
        {
            if (consumer != null)
                throw new InvalidOperationException("Consumer has already started");

            consumer = internalConsumerFactory.CreateConsumer(configuration);
            consumer.CancelledAsync += InternalConsumerOnCancelledAsync;
            consumer.ChannelFaultedAsync += InternalConsumerOnChannelFaultedAsync;
        }

        var status = await consumer.StartConsumingAsync(cancellationToken: cancellationToken);
        foreach (var queue in status.Started)
            await eventBus.PublishAsync(new StartConsumingSucceededEvent(this, queue));
        foreach (var queue in status.Failed)
            await eventBus.PublishAsync(new StartConsumingFailedEvent(this, queue));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed) return;

        disposed = true;

        var consumerToDispose = Interlocked.Exchange(ref consumer, null);
        if (consumerToDispose == null) return;

        foreach (var disposable in disposables)
            disposable.Dispose();

        await consumerToDispose.DisposeAsync();

        await eventBus.PublishAsync(new StoppedConsumingEvent(this));
    }

    private async Task InternalConsumerOnCancelledAsync(object sender, InternalConsumerCancelledEventArgs e)
    {
        if (e.Active.Count == 0)
            await DisposeAsync();
        await Task.CompletedTask;
    }

    private async Task OnConnectionDisconnected(ConnectionDisconnectedEvent messageEvent)
    {
        if (messageEvent.Type != PersistentConnectionType.Consumer) return;

        if (consumer != null)
            await consumer.StopConsumingAsync();
    }

    private async Task OnConnectionRecovered(ConnectionRecoveredEvent messageEvent)
    {
        if (messageEvent.Type != PersistentConnectionType.Consumer) return;

        await RestartConsumingAsync();
    }

    private Task RestartConsumingPeriodically() => RestartConsumingAsync();

    private async Task InternalConsumerOnChannelFaultedAsync(object sender, AsyncEventArgs e)
    {
        var last = Interlocked.Read(ref lastChannelFaultRestartTimestamp);
        var now = Stopwatch.GetTimestamp();
        if (last != 0 && Internals.StopwatchHelper.GetElapsedTime(last, now) < MinChannelFaultRestartInterval) return;
        if (Interlocked.CompareExchange(ref lastChannelFaultRestartTimestamp, now, last) != last) return;

        try
        {
            await RestartConsumingAsync();
        }
        catch (Exception exception)
        {
            // this runs detached from the channel's event loop - nothing above it observes a failure
            logger.FailedToRestartAfterChannelFault(exception);
        }
    }

    private async Task RestartConsumingAsync()
    {
        var consumerToRestart = consumer;
        if (consumerToRestart == null) return;

        var status = await consumerToRestart.StartConsumingAsync(false);

        foreach (var queue in status.Started)
            await eventBus.PublishAsync(new StartConsumingSucceededEvent(this, queue));
        foreach (var queue in status.Failed)
            await eventBus.PublishAsync(new StartConsumingFailedEvent(this, queue));

        if (ContainsOnlyFailedExclusiveQueues(status))
            await DisposeAsync();
    }

    private static bool ContainsOnlyFailedExclusiveQueues(InternalConsumerStatus status)
    {
        return status.Active.Count == 0 && status.Failed.Count > 0 && status.Failed.All(x => x.IsExclusive);
    }
}
