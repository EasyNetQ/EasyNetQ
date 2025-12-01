using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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
public class InternalConsumerCancelledEventArgs : AsyncEventArgs
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
public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e) where TEventArgs : AsyncEventArgs;

/// <summary>
///     Consumer which starts/stops raw consumers
/// </summary>
public interface IInternalConsumer : IAsyncDisposable
{
    /// <summary>
    ///     Starts consuming
    /// </summary>
    Task<InternalConsumerStatus> StartConsumingAsync(bool firstStart = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stops consuming
    /// </summary>
    Task StopConsumingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Raised when consumer is cancelled
    /// </summary>
    event AsyncEventHandler<InternalConsumerCancelledEventArgs> CancelledAsync;
}

/// <inheritdoc />
#pragma warning disable IDISP026
public class InternalConsumer : IInternalConsumer
#pragma warning restore IDISP026
{
    private readonly Dictionary<string, AsyncBasicConsumer> consumers = new();
    private readonly AsyncLock mutex = new();

    private readonly ConsumerConfiguration configuration;
    private readonly IConsumerConnection connection;
    private readonly IEventBus eventBus;
    private readonly IServiceProvider serviceResolver;
    private readonly ILogger<InternalConsumer> logger;

    private volatile bool disposed;
    private IChannel channel;

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
    public async Task<InternalConsumerStatus> StartConsumingAsync(bool firstStart, CancellationToken cancellationToken = default)
    {
        if (disposed) throw new ObjectDisposedException(nameof(InternalConsumer));

        using (await mutex.AcquireAsync(cancellationToken))
        {
            if (IsChannelClosedWithSoftError(channel))
            {
                logger.LogInformation("Channel has shutdown with soft error and will be recreated");

                foreach (var consumer in consumers.Values)
                {
                    consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                    await consumer.DisposeAsync();
                }

                consumers.Clear();
                if (channel != null)
                    await channel.DisposeAsync();
                channel = null;
            }

            if (channel == null)
            {
                try
                {
                    channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                    await channel.BasicQosAsync(0, configuration.PrefetchCount, false, cancellationToken);
                }
                catch (Exception exception)
                {
                    if (channel != null)
                        await channel.DisposeAsync();
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
                    var arguments = perQueueConfiguration.Arguments ?? new Dictionary<string, object>();
                    consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                    var consumerTag = await channel.BasicConsumeAsync(
                        queue.Name, // queue
                        perQueueConfiguration.AutoAck, // noAck
                        perQueueConfiguration.ConsumerTag, // consumerTag
                        true, // noLocal
                        perQueueConfiguration.IsExclusive, // exclusive
                        arguments, // arguments
                        consumer, // consumer
                        cancellationToken // cancellationToken
                    );
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
    }

    /// <inheritdoc />
    public async Task StopConsumingAsync(CancellationToken cancellationToken = default)
    {
        if (disposed) throw new ObjectDisposedException(nameof(InternalConsumer));

        using (await mutex.AcquireAsync(cancellationToken))
        {
            foreach (var consumer in consumers.Values)
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                foreach (var consumerTag in consumer.ConsumerTags)
                {
                    try
                    {
                        if (channel != null)
                            await channel.BasicCancelAsync(consumerTag, false, cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(
                            exception,
                            "Failed to stop consuming on consumerTag {consumerTag}",
                            consumerTag
                        );
                    }
                }

                await consumer.DisposeAsync();
            }

            consumers.Clear();
        }
    }

    /// <inheritdoc />
    public event AsyncEventHandler<InternalConsumerCancelledEventArgs> CancelledAsync;

    public virtual async ValueTask DisposeAsync()
    {
        if (disposed) return;

        disposed = true;

        using (await mutex.AcquireAsync())
        {
            foreach (var consumer in consumers.Values)
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                foreach (var consumerTag in consumer.ConsumerTags)
                {
                    try
                    {
                        if (channel != null)
                            await channel.BasicCancelAsync(consumerTag);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to dispose on consumerTag {consumerTag}",
                            consumerTag
                        );
                    }
                }

                await consumer.DisposeAsync();
            }

            consumers.Clear();
            if (channel != null)
                await channel.DisposeAsync();
        }
    }

    private void AsyncBasicConsumerOnConsumerCancelled(object sender, ConsumerEventArgs messageEvent)
    {
        _ = HandleConsumerCancelledAsync(sender, messageEvent);
    }

    private async Task HandleConsumerCancelledAsync(object sender, ConsumerEventArgs messageEvent)
    {
        Queue cancelled;
        IReadOnlyCollection<Queue> active;
        using (await mutex.AcquireAsync().ConfigureAwait(false))
        {
            if (sender is AsyncBasicConsumer consumer && consumers.Remove(consumer.Queue.Name))
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                cancelled = consumer.Queue;
                active = consumers.Select(x => x.Value.Queue).ToList();
                await consumer.DisposeAsync();
                if (IsChannelClosedWithSoftError(channel)) return;
                await consumer.DisposeAsync();
#pragma warning restore IDISP007
                if (IsChannelClosedWithSoftError(channel)) return;
            }
            else
            {
                return;
            }
        }

        if (CancelledAsync != null)
        {
            await CancelledAsync.Invoke(this, new InternalConsumerCancelledEventArgs(cancelled, active));
        }
    }

    private static bool IsChannelClosedWithSoftError(IChannel channel)
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
