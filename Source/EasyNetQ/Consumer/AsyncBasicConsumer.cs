using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Pipeline;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Consumer;

internal sealed class AsyncBasicConsumer : AsyncDefaultBasicConsumer, IAsyncDisposable
{
    private readonly CancellationTokenSource cts = new();
    private readonly IEventBus eventBus;
    private readonly ConsumerContext consumerContext;
    private readonly PipelineStep<ConsumeContext> pipeline;
    private readonly ILogger<InternalConsumer> logger;
    private readonly Queue queue;
    private readonly bool autoAck;

    private volatile bool disposed;

    public AsyncBasicConsumer(
        ILogger<InternalConsumer> logger,
        IChannel channel,
        Queue queue,
        bool autoAck,
        IEventBus eventBus,
        ConsumerContext consumerContext
    ) : base(channel)
    {
        this.logger = logger;
        this.queue = queue;
        this.autoAck = autoAck;
        this.eventBus = eventBus;
        this.consumerContext = consumerContext;
        pipeline = consumerContext.MessagePipeline;
    }

    public Queue Queue => queue;

    public event EventHandler<ConsumerEventArgs> ConsumerCancelled;

    /// <inheritdoc />
    protected override async Task OnCancelAsync(string[] consumerTags, CancellationToken cancellationToken = default)
    {
        await base.OnCancelAsync(consumerTags, cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.ConsumerCancelled(string.Join(", ", consumerTags));
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

        var context = consumerContext.RentMessageContext();
        try
        {
            context.ReceivedInfo = new MessageReceivedInfo(consumerTag, deliveryTag, redelivered, exchange, routingKey, queue.Name);
            context.Properties = new MessageProperties(properties);
            context.Body = body;
            context.CancellationToken = cts.Token;

            await pipeline(context).ConfigureAwait(false);

            if (!autoAck)
                await ApplyAckAsync(context.Ack, context.ReceivedInfo, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            consumerContext.ReturnMessageContext(context);
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

    private async ValueTask ApplyAckAsync(AckDecision decision, MessageReceivedInfo receivedInfo, CancellationToken cancellationToken)
    {
        try
        {
            switch (decision)
            {
                case AckDecision.Ack:
                    await Channel.BasicAckAsync(receivedInfo.DeliveryTag, false, cancellationToken).ConfigureAwait(false);
                    break;
                case AckDecision.NackRequeue:
                    await Channel.BasicNackAsync(receivedInfo.DeliveryTag, false, true, cancellationToken).ConfigureAwait(false);
                    break;
                case AckDecision.NackDiscard:
                    await Channel.BasicNackAsync(receivedInfo.DeliveryTag, false, false, cancellationToken).ConfigureAwait(false);
                    break;
                case AckDecision.Handled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(decision), decision, "Unknown ack decision");
            }
        }
        catch (AlreadyClosedException alreadyClosedException)
        {
            logger.FailedToAckOrNack(alreadyClosedException, receivedInfo.ConsumerTag, receivedInfo.DeliveryTag, receivedInfo.Queue);
        }
        catch (IOException ioException)
        {
            logger.FailedToAckOrNack(ioException, receivedInfo.ConsumerTag, receivedInfo.DeliveryTag, receivedInfo.Queue);
        }
        catch (Exception exception)
        {
            logger.UnexpectedExceptionOnAckOrNack(exception, receivedInfo.ConsumerTag, receivedInfo.DeliveryTag, receivedInfo.Queue);
        }
    }
}
