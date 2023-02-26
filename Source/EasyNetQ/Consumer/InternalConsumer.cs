using EasyNetQ.DI;
using EasyNetQ.Internals;
using EasyNetQ.Logging;
using EasyNetQ.Persistent;
using EasyNetQ.Topology;
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
    public InternalConsumerCancelledEventArgs(in Queue cancelled, IReadOnlyCollection<Queue> active)
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
    void StopConsuming();

    /// <summary>
    ///     Raised when consumer is cancelled
    /// </summary>
    event EventHandler<InternalConsumerCancelledEventArgs>? Cancelled;
}

/// <inheritdoc />
public class InternalConsumer : IInternalConsumer
{
    private readonly Dictionary<string, AsyncBasicConsumer> consumers = new();
    private readonly AsyncLock mutex = new();

    private readonly ConsumerConfiguration configuration;
    private readonly IConsumerConnection connection;
    private readonly IEventBus eventBus;
    private readonly IServiceResolver serviceResolver;
    private readonly ILogger logger;

    private volatile bool disposed;
    private IModel? model;

    /// <summary>
    ///     Creates InternalConsumer
    /// </summary>
    public InternalConsumer(
        IServiceResolver serviceResolver,
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

        if (IsModelClosedWithSoftError(model))
        {
            logger.Info("Model has shutdown with soft error and will be recreated");

            foreach (var consumer in consumers.Values)
            {
                consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
                consumer.Dispose();
            }

            consumers.Clear();

            model?.Dispose();
            model = null;
        }

        if (model == null)
        {
            try
            {
                model = connection.CreateModel();
                model.DefaultConsumer = NoopDefaultConsumer.Instance;
                model.BasicQos(0, configuration.PrefetchCount, false);
            }
            catch (Exception exception)
            {
                model?.Dispose();
                model = null;

                logger.Error(exception, "Failed to create model");
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
                    model,
                    queue,
                    perQueueConfiguration.AutoAck,
                    eventBus,
                    perQueueConfiguration.ConsumeDelegate
                );
                consumer.ConsumerCancelled += AsyncBasicConsumerOnConsumerCancelled;
                var consumerTag = model.BasicConsume(
                    queue.Name, // queue
                    perQueueConfiguration.AutoAck, // noAck
                    perQueueConfiguration.ConsumerTag, // consumerTag
                    true, // noLocal
                    perQueueConfiguration.IsExclusive, // exclusive
                    perQueueConfiguration.Arguments, // arguments
                    consumer // consumer
                );
                consumers.Add(queue.Name, consumer);

                logger.InfoFormat(
                    "Declared consumer with consumerTag {consumerTag} on queue {queue}",
                    consumerTag,
                    queue.Name
                );

                startedQueues.Add(queue);
                activeQueues.Add(queue);
            }
            catch (Exception exception)
            {
                logger.Error(
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
    public void StopConsuming()
    {
        if (disposed) throw new ObjectDisposedException(nameof(InternalConsumer));

        using var _ = mutex.Acquire();

        foreach (var consumer in consumers.Values)
        {
            consumer.ConsumerCancelled -= AsyncBasicConsumerOnConsumerCancelled;
            foreach (var consumerTag in consumer.ConsumerTags)
            {
                try
                {
                    model?.BasicCancelNoWait(consumerTag);
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
    public event EventHandler<InternalConsumerCancelledEventArgs>? Cancelled;

    /// <inheritdoc />
    public void Dispose()
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
                    model?.BasicCancelNoWait(consumerTag);
                }
                catch (AlreadyClosedException)
                {
                }
            }

            consumer.Dispose();
        }

        consumers.Clear();
        model?.Dispose();
    }

    private async Task AsyncBasicConsumerOnConsumerCancelled(object sender, ConsumerEventArgs @event)
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

                if (IsModelClosedWithSoftError(model)) return;
            }
            else
            {
                return;
            }
        }

        Cancelled?.Invoke(this, new InternalConsumerCancelledEventArgs(cancelled, active));
    }

    private static bool IsModelClosedWithSoftError(IModel? model)
    {
        var closeReason = model?.CloseReason;
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
