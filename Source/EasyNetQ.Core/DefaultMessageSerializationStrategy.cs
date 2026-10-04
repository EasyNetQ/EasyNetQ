using EasyNetQ.Internals;

namespace EasyNetQ;

/// <inheritdoc />
public class DefaultMessageSerializationStrategy : IMessageSerializationStrategy
{
    private readonly IMessageTypeRegistry registry;
    private readonly IMessageSerializer serializer;
    private readonly ICorrelationIdGenerationStrategy correlationIdGenerator;

    /// <summary>
    ///     Creates DefaultMessageSerializationStrategy
    /// </summary>
    public DefaultMessageSerializationStrategy(
        IMessageTypeRegistry registry,
        IMessageSerializer serializer,
        ICorrelationIdGenerationStrategy correlationIdGenerator
    )
    {
        this.registry = registry;
        this.serializer = serializer;
        this.correlationIdGenerator = correlationIdGenerator;
    }

    /// <inheritdoc />
    public SerializedMessage SerializeMessage(IMessage message)
    {
        var descriptor = registry.GetOrAdd(message.MessageType);
        var body = message.GetBody();
        var messageBody = body is null
            ? Internals.EmptyMemoryOwner.Instance
            : descriptor.SerializeBody(serializer, body);
        return new SerializedMessage(StampProperties(message.Properties, descriptor), messageBody);
    }

    /// <inheritdoc />
    public SerializedMessage SerializeMessage<T>(T body, in MessageProperties properties)
    {
        if (body is null || body.GetType() != typeof(T))
            return SerializeMessage(new Message<T>(body!, properties));

        var descriptor = registry.GetOrAdd<T>();
        return new SerializedMessage(StampProperties(properties, descriptor), serializer.Serialize(body, descriptor));
    }

    /// <inheritdoc />
    public IMessage DeserializeMessage(in MessageProperties properties, in ReadOnlyMemory<byte> body)
    {
        var descriptor = registry.GetByWireName(properties.Type!);
        var messageBody = body.IsEmpty ? null : descriptor.DeserializeBody(serializer, body);
        return descriptor.CreateMessage(messageBody, properties);
    }

    private MessageProperties StampProperties(in MessageProperties properties, MessageTypeDescriptor descriptor)
        => properties with
        {
            Type = descriptor.WireName,
            CorrelationId = string.IsNullOrEmpty(properties.CorrelationId)
                ? correlationIdGenerator.GetCorrelationId()
                : properties.CorrelationId
        };
}
