using EasyNetQ.Events;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer;

internal class AsyncBasicConsumer : AsyncDefaultBasicConsumer, IDisposable
{
    private readonly CancellationTokenSource cts = new();
    private readonly IEventBus eventBus;
    private readonly ConsumeDelegate consumeDelegate;
    private readonly IServiceProvider serviceResolver;
    private readonly ILogger logger;
    private readonly Queue queue;
    private readonly bool autoAck;

    private volatile bool disposed;

    public AsyncBasicConsumer(
        IServiceProvider serviceResolver,
        ILogger logger,
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

    public event EventHandler<ConsumerEventArgs>? ConsumerCancelled;

    /// <inheritdoc />
    public override async Task OnCancel(params string[] consumerTags)
    {
        await base.OnCancel(consumerTags).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
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
        ReadOnlyMemory<byte> body
    )
    {
        if (cts.IsCancellationRequested)
            return;

        if (logger.IsEnabled(LogLevel.Debug))
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
        eventBus.Publish(new DeliveredMessageEvent(messageReceivedInfo, messageProperties, messageBody));
        var ackStrategy = await consumeDelegate(new ConsumeContext(messageReceivedInfo, messageProperties, messageBody, serviceResolver, cts.Token)).ConfigureAwait(false);
        if (!autoAck)
        {
            var ackResult = await AckAsync(ackStrategy, messageReceivedInfo);
            eventBus.Publish(new AckEvent(messageReceivedInfo, messageProperties, messageBody, ackResult));
        }
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        cts.Cancel();
        cts.Dispose();
        eventBus.Publish(new ConsumerChannelDisposedEvent(ConsumerTags));
    }

    private async Task<AckResult> AckAsync(
        AckStrategyAsync ackStrategy,
        MessageReceivedInfo receivedInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (Channel != null)
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
