namespace EasyNetQ.Pipeline;

/// <summary>
///     Message layer of the consume pipeline: everything known about one delivery, plus the acknowledgement decision
///     the pipeline produces. Instances are pooled per consumer; transports may derive typed contexts.
/// </summary>
public class ConsumeContext : LayerContext
{
    /// <summary>
    ///     Creates a message context for <paramref name="consumer" />
    /// </summary>
    public ConsumeContext(ConsumerContext consumer) : base(consumer)
    {
        Consumer = consumer;
    }

    /// <summary>
    ///     The consumer the message was delivered by (read-only view)
    /// </summary>
    public IConsumerView Consumer { get; }

    /// <summary>
    ///     The channel of the consumer (read-only view)
    /// </summary>
    public IChannelView Channel => Consumer.Channel;

    /// <summary>
    ///     The connection of the consumer (read-only view)
    /// </summary>
    public IConnectionView Connection => Consumer.Channel.Connection;

    /// <summary>
    ///     Delivery information (consumer tag, delivery tag, exchange, routing key, ...)
    /// </summary>
    public MessageReceivedInfo ReceivedInfo { get; set; }

    /// <summary>
    ///     Message properties as received; middleware may replace them (e.g. after decrypting/decompressing)
    /// </summary>
    public MessageProperties Properties { get; set; }

    /// <summary>
    ///     Message body as received; middleware may replace it
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; set; }

    /// <summary>
    ///     The message type resolved from the wire type name, set by <see cref="Middleware.ResolveMessageTypeStep" />
    /// </summary>
    public MessageTypeDescriptor? MessageType { get; set; }

    /// <summary>
    ///     The handler resolved for <see cref="MessageType" />, set by <see cref="Middleware.ResolveHandlerStep" />
    /// </summary>
    public HandlerEntry? Handler { get; set; }

    /// <summary>
    ///     The serializer for this message, set by <see cref="Middleware.SelectSerializerStep" />
    /// </summary>
    public IMessageSerializer? Serializer { get; set; }

    /// <summary>
    ///     The deserialized message body, set by <see cref="Middleware.DeserializeStep" />; middleware between
    ///     deserialization and dispatch can inspect or replace it
    /// </summary>
    public object? Message { get; set; }

    /// <summary>
    ///     The acknowledgement the transport applies once the pipeline completes. Defaults to <see cref="AckDecision.Ack" />;
    ///     handlers and error handling set it to something else.
    /// </summary>
    public AckDecision Ack { get; set; } = AckDecision.Ack;

    /// <summary>
    ///     The exception that failed the message, set by error handling before the error strategy runs
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    ///     Cancelled when the consumer stops
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <inheritdoc />
    protected internal override void Reset()
    {
        base.Reset();
        ReceivedInfo = default;
        Properties = default;
        Body = default;
        MessageType = null;
        Handler = null;
        Serializer = null;
        Message = null;
        Ack = AckDecision.Ack;
        Error = null;
        CancellationToken = default;
    }
}
