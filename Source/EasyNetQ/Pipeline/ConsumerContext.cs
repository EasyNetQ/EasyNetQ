namespace EasyNetQ.Pipeline;

/// <summary>
///     Consumer layer: one per consumed queue. Owns the built message pipeline and the pool of
///     <see cref="ConsumeContext" /> instances the transport rents for each delivery.
/// </summary>
public sealed class ConsumerContext : LayerContext, IConsumerView
{
    private readonly ContextPool<ConsumeContext> messagePool;

    /// <summary>
    ///     Creates a consumer context for <paramref name="queue" /> on <paramref name="channel" />
    /// </summary>
    /// <param name="channel">The channel the consumer runs on</param>
    /// <param name="queue">The queue being consumed</param>
    /// <param name="messageContextFactory">
    ///     Creates the per-delivery context; transports supply this to use a derived, transport-typed context
    /// </param>
    public ConsumerContext(ChannelContext channel, string queue, Func<ConsumerContext, ConsumeContext>? messageContextFactory = null)
        : base(channel)
    {
        Channel = channel;
        Queue = queue;
        var factory = messageContextFactory ?? (consumer => new ConsumeContext(consumer));
        messagePool = new ContextPool<ConsumeContext>(() => factory(this));
    }

    /// <inheritdoc />
    public IChannelView Channel { get; }

    /// <inheritdoc />
    public string Queue { get; }

    /// <inheritdoc />
    public ushort PrefetchCount { get; set; }

    /// <inheritdoc />
    public bool AutoAck { get; set; }

    /// <summary>
    ///     The message pipeline run for every delivery on this consumer
    /// </summary>
    public PipelineStep<ConsumeContext> MessagePipeline { get; set; } = static _ => default;

    /// <summary>
    ///     Rents a message context for a delivery; return it with <see cref="ReturnMessageContext" />
    /// </summary>
    public ConsumeContext RentMessageContext() => messagePool.Rent();

    /// <summary>
    ///     Returns a message context after the delivery has been fully processed (including acknowledgement)
    /// </summary>
    public void ReturnMessageContext(ConsumeContext context) => messagePool.Return(context);
}
