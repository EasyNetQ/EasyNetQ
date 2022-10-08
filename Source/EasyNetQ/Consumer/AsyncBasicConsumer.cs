using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Logging;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer;

internal class AsyncBasicConsumer : AsyncDefaultBasicConsumer, IDisposable
{
    private readonly CancellationTokenSource cts = new();
    private readonly AsyncCountdownEvent onTheFlyMessages = new();

    private readonly IEventBus eventBus;
    private readonly IHandlerRunner handlerRunner;
    private readonly MessageHandler messageHandler;
    private readonly long consumerId;
    private readonly ILogger logger;
    private readonly Queue queue;
    private readonly bool autoAck;

    private volatile bool disposed;

    public AsyncBasicConsumer(
        long consumerId,
        ILogger logger,
        IModel model,
        Queue queue,
        bool autoAck,
        IEventBus eventBus,
        IHandlerRunner handlerRunner,
        MessageHandler messageHandler
    ) : base(model)
    {
        this.consumerId = consumerId;
        this.logger = logger;
        this.queue = queue;
        this.autoAck = autoAck;
        this.eventBus = eventBus;
        this.handlerRunner = handlerRunner;
        this.messageHandler = messageHandler;
    }

    public Queue Queue => queue;

    /// <inheritdoc />
    public override async Task OnCancel(params string[] consumerTags)
    {
        await base.OnCancel(consumerTags).ConfigureAwait(false);

        if (logger.IsInfoEnabled())
        {
            logger.InfoFormat(
                "Consumer {consumerId} cancelled for consumerTags {@consumerTags}",
                consumerId,
                consumerTags
            );
        }
    }

    /// <inheritdoc />
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

        var messageReceivedInfo = new MessageReceivedInfo(
            consumerTag, deliveryTag, redelivered, exchange, routingKey, queue.Name
        );
        var messageBody = body;
        var messageProperties = new MessageProperties();
        messageProperties.CopyFrom(properties);

        if (logger.IsDebugEnabled())
        {
            logger.DebugFormat(
                "Message with properties {@properties} delivered to consumer {consumerId} with receivedInfo {@receivedInfo}",
                messageProperties,
                consumerId,
                messageReceivedInfo
            );
        }

        onTheFlyMessages.Increment();
        try
        {
            eventBus.Publish(new DeliveredMessageEvent(messageReceivedInfo, messageProperties, messageBody));
            var context = new ConsumerExecutionContext(
                messageHandler, messageReceivedInfo, messageProperties, messageBody
            );
            var ackStrategy = await handlerRunner.InvokeUserMessageHandlerAsync(
                context, cts.Token
            ).ConfigureAwait(false);

            if (!autoAck)
            {
                var ackResult = Ack(ackStrategy, messageReceivedInfo, messageProperties);
                eventBus.Publish(new AckEvent(messageReceivedInfo, messageProperties, messageBody, ackResult));
            }
        }
        finally
        {
            onTheFlyMessages.Decrement();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        cts.Cancel();
        onTheFlyMessages.Wait();
        cts.Dispose();
        onTheFlyMessages.Dispose();
        eventBus.Publish(new ConsumerModelDisposedEvent(ConsumerTags));
    }

    private AckResult Ack(AckStrategy ackStrategy, MessageReceivedInfo receivedInfo, MessageProperties properties)
    {
        try
        {
            return ackStrategy(Model, receivedInfo.DeliveryTag);
        }
        catch (AlreadyClosedException alreadyClosedException)
        {
            logger.Info(
                alreadyClosedException,
                "Consumer {consumerId} failed to ACK/NACK a message with properties {@properties} and receivedInfo {@receivedInfo}",
                consumerId,
                properties,
                receivedInfo
            );
        }
        catch (IOException ioException)
        {
            logger.Info(
                ioException,
                "Consumer {consumerId} failed to ACK/NACK a message with properties {@properties} and receivedInfo {@receivedInfo}",
                properties,
                consumerId,
                receivedInfo
            );
        }
        catch (Exception exception)
        {
            logger.Error(
                exception,
                "Consumer {consumerId} unexpectedly failed to ACK/NACK a message with properties {@properties} and receivedInfo {@receivedInfo}",
                properties,
                consumerId,
                receivedInfo
            );
        }

        return AckResult.Exception;
    }
}
