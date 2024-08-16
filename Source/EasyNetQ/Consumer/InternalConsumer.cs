using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer;

/// <summary>
///     Represents an internal consumer's status: which queues are consuming and which are not
/// </summary>
public readonly struct InternalConsumerStatus
{
    /// <summary>
    ///     Creates InternalConsumerStatus
    /// </summary>
    public InternalConsumerStatus(IReadOnlyCollection<Queue> started, IReadOnlyCollection<Queue> active, IReadOnlyCollection<Queue> failed)
    {
        Started = started;
        Active = active;
        Failed = failed;
    }

    /// <summary>
    ///     Queues with active consumers
    /// </summary>
    public IReadOnlyCollection<Queue> Active { get; }

    /// <summary>
    ///     Queues with newly started consumers
    /// </summary>
    public IReadOnlyCollection<Queue> Started { get; }

    /// <summary>
    ///     Queues with failed consumers
    /// </summary>
    public IReadOnlyCollection<Queue> Failed { get; }
}

/// <summary>
///     Represents an internal consumer's cancelled event
/// </summary>
public class InternalConsumerCancelledEventArgs : EventArgs
{
    /// <inheritdoc />
    public InternalConsumerCancelledEventArgs(Queue cancelled, IReadOnlyCollection<Queue> active)
    {
        Cancelled = cancelled;
        Active = active;
    }

    /// <summary>
    ///     Queue for which consume is cancelled
    /// </summary>
    public Queue Cancelled { get; }

    /// <summary>
    ///     Queues for which consume is active
    /// </summary>
    public IReadOnlyCollection<Queue> Active { get; }
}

/// <summary>
///     Asynchronous event handler delegate
/// </summary>
public delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e) where TEventArgs : EventArgs;

/// <summary>
///     Consumer which starts/stops raw consumers
/// </summary>
public interface IInternalConsumer : IDisposable
{
    /// <summary>
    ///     Starts consuming
    /// </summary>
    InternalConsumerStatus StartConsuming(bool firstStart = true);

    /// <summary>
    ///     Stops consuming
    /// </summary>
    Task StopConsuming();

    /// <summary>
    ///     Raised when consumer is cancelled
    /// </summary>
    event AsyncEventHandler<InternalConsumerCancelledEventArgs>? CancelledAsync;
}

/// <inheritdoc />
public class InternalConsumer : IInternalConsumer
{
    private readonly Dictionary<string, AsyncBasicConsumer> consumers = new();
    private readonly AsyncLock mutex = new();

    private readonly ConsumerConfiguration configuration;
    private readonly IConsumerConnection connection;
    private readonly IEventBus eventBus;
    private readonly IServiceProvider serviceResolver;
    private readonly ILogger logger;

    private volatile bool disposed;
    private IChannel? channel;

    /// <summary>
    ///     Creates InternalConsumer
    /// </summary>
    public InternalConsumer(
        IServiceProvider serviceResolver,
        ILogger<InternalConsumer> logger,
        ConsumerConfiguration configuration,
        IConsumerConnection connection,
        IEventBus eventBus
    )
    {
        this.serviceResolver = serviceResolver;
        this.logger = logger;
        this.configuration = configuration;
        this.connection = connection;
        this.eventBus = eventBus;
    }

    /// <inheritdoc />
    public InternalConsumerStatus StartConsuming(bool firstStart)
    {
        if (disposed) throw new ObjectDisposedException(nameof(InternalConsumer));

        using var _ = mutex.Acquire();

        if (IsChannelClosedWithSoftError(channel))
        {
            logger.LogInformation("Channel has shutdown with soft error and will be recreated");

            foreach (var consumer in consumers.Values)
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                consumer.Dispose();
            }

            consumers.Clear();
            channel?.Dispose();
            channel = null;
        }

        if (channel == null)
        {
            try
            {
                channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
                channel.BasicQosAsync(0, configuration.PrefetchCount, false).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                channel?.Dispose();
                channel = null;

                logger.LogError(exception, "Failed to create channel");
                return new InternalConsumerStatus(Array.Empty<Queue>(), Array.Empty<Queue>(), Array.Empty<Queue>());
            }
        }

        var startedQueues = new HashSet<Queue>();
        var activeQueues = new HashSet<Queue>();
        var failedQueues = new HashSet<Queue>();

        foreach (var kvp in configuration.PerQueueConfigurations)
        {
            var queue = kvp.Key;
            var perQueueConfiguration = kvp.Value;

            if (
                consumers.TryGetValue(queue.Name, out var alreadyStartedConsumer)
                && (!queue.IsExclusive || alreadyStartedConsumer.IsRunning)
            )
            {
                activeQueues.Add(queue);
                continue;
            }

            if (queue.IsExclusive && !firstStart)
            {
                failedQueues.Add(queue);
                continue;
            }

            try
            {
                var consumer = new AsyncBasicConsumer(
                    serviceResolver,
                    logger,
                    channel,
                    queue,
                    perQueueConfiguration.AutoAck,
                    eventBus,
                    perQueueConfiguration.ConsumeDelegate
                );
                var arguments = perQueueConfiguration.Arguments as IDictionary<string, object?> ?? new Dictionary<string, object?>();
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                var consumerTag = channel.BasicConsumeAsync(
                    queue.Name, // queue
                    perQueueConfiguration.AutoAck, // noAck
                    perQueueConfiguration.ConsumerTag, // consumerTag
                    true, // noLocal
                    perQueueConfiguration.IsExclusive, // exclusive
                    arguments, // arguments
                    consumer // consumer
                ).GetAwaiter().GetResult();
                consumers.Add(queue.Name, consumer);

                logger.LogInformation(
                    "Declared consumer with consumerTag {consumerTag} on queue {queue}",
                    consumerTag,
                    queue.Name
                );

                startedQueues.Add(queue);
                activeQueues.Add(queue);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to declare consumer on queue {queue}",
                    queue.Name
                );

                failedQueues.Add(queue);
            }
        }

        return new InternalConsumerStatus(startedQueues, activeQueues, failedQueues);
    }

    /// <inheritdoc />
    public async Task StopConsuming()
    {
        if (disposed) throw new ObjectDisposedException(nameof(InternalConsumer));

        using var _ = await mutex.AcquireAsync();

        foreach (var consumer in consumers.Values)
        {
            consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
            foreach (var consumerTag in consumer.ConsumerTags)
            {
                try
                {
                    if(channel != null)
                        await channel.BasicCancelAsync(consumerTag);
                }
                catch (AlreadyClosedException)
                {
                }
            }

            consumer.Dispose();
        }

        consumers.Clear();
    }

    /// <inheritdoc />
    public event AsyncEventHandler<InternalConsumerCancelledEventArgs>? CancelledAsync;

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (disposed) return;

        disposed = true;

        using var _ = mutex.Acquire();

        foreach (var consumer in consumers.Values)
        {
            consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
            foreach (var consumerTag in consumer.ConsumerTags)
            {
                try
                {
                    channel?.BasicCancelAsync(consumerTag).GetAwaiter().GetResult();
                }
                catch (AlreadyClosedException)
                {
                }
            }

            consumer.Dispose();
        }

        consumers.Clear();
        channel?.Dispose();
    }

    private void AsyncBasicConsumerOnConsumerCancelled(object? sender, ConsumerEventArgs @event)
    {
        _ = HandleConsumerCancelledAsync(sender, @event);
    }

    private async Task HandleConsumerCancelledAsync(object? sender, ConsumerEventArgs @event)
    {
        Queue cancelled;
        IReadOnlyCollection<Queue> active;
        using (await mutex.AcquireAsync().ConfigureAwait(false))
        {
            if (sender is AsyncBasicConsumer consumer && consumers.Remove(consumer.Queue.Name))
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                consumer.Dispose();
                cancelled = consumer.Queue;
                active = consumers.Select(x => x.Value.Queue).ToList();

                if (IsChannelClosedWithSoftError(channel)) return;
            }
            else
            {
                return;
            }
        }

        if (CancelledAsync != null)
        {
            await CancelledAsync.Invoke(this, new InternalConsumerCancelledEventArgs(cancelled, active)).ConfigureAwait(false);
        }
    }

    private static bool IsChannelClosedWithSoftError(IChannel? channel)
    {
        var closeReason = channel?.CloseReason;
        if (closeReason == null) return false;

        return closeReason.ReplyCode switch
        {
            AmqpErrorCodes.PreconditionFailed => true,
            AmqpErrorCodes.ResourceLocked => true,
            AmqpErrorCodes.AccessRefused => true,
            AmqpErrorCodes.NotFound => true,
            _ => false
        };
    }
}
