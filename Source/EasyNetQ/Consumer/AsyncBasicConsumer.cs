using EasyNetQ.Events;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer;

internal sealed class AsyncBasicConsumer : AsyncDefaultBasicConsumer, IAsyncDisposable
{
    private readonly CancellationTokenSource cts = new();
    private readonly TaskCompletionSource<bool> runningHandlersCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IEventBus eventBus;
    private readonly ConsumeDelegate consumeDelegate;
    private readonly IServiceProvider services;
    private readonly ILogger<InternalConsumer> logger;
    private readonly Queue queue;
    private readonly bool autoAck;

    private volatile bool disposed;
    private int runningHandlers;

    public AsyncBasicConsumer(
        IServiceProvider services,
        ILogger<InternalConsumer> logger,
        IChannel channel,
        Queue queue,
        bool autoAck,
        IEventBus eventBus,
        ConsumeDelegate consumeDelegate
    ) : base(channel)
    {
        this.services = services;
        this.logger = logger;
        this.queue = queue;
        this.autoAck = autoAck;
        this.eventBus = eventBus;
        this.consumeDelegate = consumeDelegate;
    }

    public Queue Queue => queue;

    public event EventHandler<ConsumerEventArgs> ConsumerCancelled;

    /// <inheritdoc />
    protected override async Task OnCancelAsync(string[] consumerTags, CancellationToken cancellationToken = default)
    {
        await base.OnCancelAsync(consumerTags, cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Consumer with consumerTags {ConsumerTags} has cancelled",
                string.Join(", ", consumerTags)
            );
        }

        ConsumerCancelled?.Invoke(this, new ConsumerEventArgs(consumerTags));
    }

    public override async Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    )
    {
        // Counted before checking the cancellation, so that a handler either sees it and returns
        // or is waited for by WaitForRunningHandlersAsync
        Interlocked.Increment(ref runningHandlers);
        try
        {
            if (cts.IsCancellationRequested)
                return;

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Message delivered to consumer {ConsumerTag} with deliveryTag {DeliveryTag}",
                    consumerTag,
                    deliveryTag
                );
            }

            var messageBody = body;
            var messageReceivedInfo = new MessageReceivedInfo(
                consumerTag, deliveryTag, redelivered, exchange, routingKey, queue.Name
            );
            var messageProperties = new MessageProperties(properties);
            await eventBus.PublishAsync(new DeliveredMessageEvent(messageReceivedInfo, messageProperties, messageBody));

            var ackStrategy = await consumeDelegate(new ConsumeContext(messageReceivedInfo, messageProperties, messageBody, services, cts.Token)).ConfigureAwait(false);

            // While the consumer is being disposed, its channel is still open and subscribed: a requeued message would
            // be delivered to it again, and on a quorum queue it would come back after the other unacked messages.
            // Closing the channel requeues it in order instead
            if (!autoAck && cts.IsCancellationRequested && ackStrategy == AckStrategies.NackWithRequeueAsync)
                return;

            if (!autoAck)
            {
                var ackResult = await AckAsync(ackStrategy, messageReceivedInfo, cancellationToken);
                await eventBus.PublishAsync(new AckEvent(messageReceivedInfo, messageProperties, messageBody, ackResult));
            }
        }
        finally
        {
            if (Interlocked.Decrement(ref runningHandlers) == 0 && cts.IsCancellationRequested)
                runningHandlersCompleted.TrySetResult(true);
        }
    }

    /// <summary>
    ///     Once the consumer is disposed, waits for the handlers that are still running
    /// </summary>
    public Task WaitForRunningHandlersAsync()
    {
        return Volatile.Read(ref runningHandlers) == 0 ? Task.CompletedTask : runningHandlersCompleted.Task;
    }

    /// <inheritdoc />
#pragma warning disable CS1998
    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;
        cts.Cancel();
        cts.Dispose();
        await eventBus.PublishAsync(new ConsumerChannelDisposedEvent(ConsumerTags));
    }
#pragma warning restore CS1998

    private async Task<AckResult> AckAsync(
        AckStrategyAsync ackStrategy,
        MessageReceivedInfo receivedInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ackStrategy(Channel, receivedInfo.DeliveryTag, cancellationToken);
        }
        catch (AlreadyClosedException alreadyClosedException)
        {
            logger.LogInformation(
                alreadyClosedException,
                "Failed to ACK or NACK, message will be retried, receivedInfo={ReceivedInfo}",
                receivedInfo
            );
        }
        catch (IOException ioException)
        {
            logger.LogInformation(
                ioException,
                "Failed to ACK or NACK, message will be retried, receivedInfo={ReceivedInfo}",
                receivedInfo
            );
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unexpected exception when attempting to ACK or NACK, receivedInfo={ReceivedInfo}",
                receivedInfo
            );
        }

        return AckResult.Exception;
    }
}
