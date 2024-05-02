using EasyNetQ.DI;
using EasyNetQ.Events;
using MS = Microsoft.Extensions.Logging;
using MSExtensions = Microsoft.Extensions.Logging.LoggerExtensions;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer;

internal class AsyncBasicConsumer : AsyncDefaultBasicConsumer, IDisposable
{
    private readonly CancellationTokenSource cts = new();
    private readonly IEventBus eventBus;
    private readonly ConsumeDelegate consumeDelegate;
    private readonly IServiceResolver serviceResolver;
    private readonly MS.ILogger logger;
    private readonly Queue queue;
    private readonly bool autoAck;

    private volatile bool disposed;

    public AsyncBasicConsumer(
        IServiceResolver serviceResolver,
        MS.ILogger logger,
        IModel model,
        Queue queue,
        bool autoAck,
        IEventBus eventBus,
        ConsumeDelegate consumeDelegate
    ) : base(model)
    {
        this.serviceResolver = serviceResolver;
        this.logger = logger;
        this.queue = queue;
        this.autoAck = autoAck;
        this.eventBus = eventBus;
        this.consumeDelegate = consumeDelegate;
    }

    public Queue Queue => queue;

    /// <inheritdoc />
    public override async Task OnCancel(params string[] consumerTags)
    {
        await base.OnCancel(consumerTags).ConfigureAwait(false);

        if (logger.IsEnabled(MS.LogLevel.Information))
        {
            MSExtensions.LogInformation(
                logger,
                "Consumer with consumerTags {consumerTags} has cancelled",
                string.Join(", ", consumerTags)
            );
        }
    }

    public override async Task HandleBasicDeliver(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IBasicProperties properties,
        ReadOnlyMemory<byte> body
    )
    {
        if (cts.IsCancellationRequested)
            return;

        if (logger.IsEnabled(MS.LogLevel.Debug))
        {
            MSExtensions.LogDebug(
                logger,
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
            var ackResult = Ack(ackStrategy, messageReceivedInfo);
            eventBus.Publish(new AckEvent(messageReceivedInfo, messageProperties, messageBody, ackResult));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        cts.Cancel();
        cts.Dispose();
        eventBus.Publish(new ConsumerModelDisposedEvent(ConsumerTags));
    }

    private AckResult Ack(AckStrategy ackStrategy, in MessageReceivedInfo receivedInfo)
    {
        try
        {
            return ackStrategy(Model, receivedInfo.DeliveryTag);
        }
        catch (AlreadyClosedException alreadyClosedException)
        {
            MSExtensions.LogInformation(
                logger,
                alreadyClosedException,
                "Failed to ACK or NACK, message will be retried, receivedInfo={receivedInfo}",
                receivedInfo
            );
        }
        catch (IOException ioException)
        {
            MSExtensions.LogInformation(
                logger,
                ioException,
                "Failed to ACK or NACK, message will be retried, receivedInfo={receivedInfo}",
                receivedInfo
            );
        }
        catch (Exception exception)
        {
            MSExtensions.LogError(
                logger,
                exception,
                "Unexpected exception when attempting to ACK or NACK, receivedInfo={receivedInfo}",
                receivedInfo
            );
        }

        return AckResult.Exception;
    }
}
