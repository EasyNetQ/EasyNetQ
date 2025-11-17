using EasyNetQ.Events;
using Microsoft.Extensions.Logging;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer;

internal sealed class AsyncBasicConsumer : AsyncDefaultBasicConsumer, IAsyncDisposable
{
    private readonly CancellationTokenSource cts = new();
    private readonly IEventBus eventBus;
    private readonly ConsumeDelegate consumeDelegate;
    private readonly IServiceProvider serviceResolver;
    private readonly ILogger<InternalConsumer> logger;
    private readonly Queue queue;
    private readonly bool autoAck;

    private volatile bool disposed;

    public AsyncBasicConsumer(
        IServiceProvider serviceResolver,
        ILogger<InternalConsumer> logger,
        IChannel channel,
        Queue queue,
        bool autoAck,
        IEventBus eventBus,
        ConsumeDelegate consumeDelegate
    ) : base(channel)
    {
        this.serviceResolver = serviceResolver;
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

        if (logger.IsInfoEnabled())
        {
            logger.LogInformation(
                "Consumer with consumerTags {consumerTags} has cancelled",
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
        if (cts.IsCancellationRequested)
            return;

        if (logger.IsDebugEnabled())
        {
            logger.LogDebug(
                "Message delivered to consumer {consumerTag} with deliveryTag {deliveryTag}",
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

        var ackStrategy = await consumeDelegate(new ConsumeContext(messageReceivedInfo, messageProperties, messageBody, serviceResolver, cts.Token)).ConfigureAwait(false);
        if (!autoAck)
        {
            var ackResult = await AckAsync(ackStrategy, messageReceivedInfo, cancellationToken);
            await eventBus.PublishAsync(new AckEvent(messageReceivedInfo, messageProperties, messageBody, ackResult));
        }
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
                "Failed to ACK or NACK, message will be retried, receivedInfo={receivedInfo}",
                receivedInfo
            );
        }
        catch (IOException ioException)
        {
            logger.LogInformation(
                ioException,
                "Failed to ACK or NACK, message will be retried, receivedInfo={receivedInfo}",
                receivedInfo
            );
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unexpected exception when attempting to ACK or NACK, receivedInfo={receivedInfo}",
                receivedInfo
            );
        }

        return AckResult.Exception;
    }
}
