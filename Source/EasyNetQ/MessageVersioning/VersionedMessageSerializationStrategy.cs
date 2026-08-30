using EasyNetQ.Internals;

namespace EasyNetQ.MessageVersioning;

/// <inheritdoc />
public class VersionedMessageSerializationStrategy : IMessageSerializationStrategy
{
    private readonly ITypeNameSerializer typeNameSerializer;
    private readonly IMessageTypeRegistry registry;
    private readonly IMessageSerializer serializer;
    private readonly ICorrelationIdGenerationStrategy correlationIdGenerator;

    /// <summary>
    ///     Creates VersionedMessageSerializationStrategy
    /// </summary>
    public VersionedMessageSerializationStrategy(
        ITypeNameSerializer typeNameSerializer,
        IMessageTypeRegistry registry,
        IMessageSerializer serializer,
        ICorrelationIdGenerationStrategy correlationIdGenerator
    )
    {
        this.typeNameSerializer = typeNameSerializer;
        this.registry = registry;
        this.serializer = serializer;
        this.correlationIdGenerator = correlationIdGenerator;
    }

    /// <inheritdoc />
    public SerializedMessage SerializeMessage(IMessage message)
    {
        var body = message.GetBody();
        var messageBody = body is null
            ? EmptyMemoryOwner.Instance
            : registry.GetOrAdd(message.MessageType).SerializeBody(serializer, body);
        var messageTypeProperty = MessageTypeProperty.CreateForMessageType(message.MessageType, typeNameSerializer);
        var messageProperties = message.Properties;
        messageProperties = messageTypeProperty.AppendTo(messageProperties);
        if (string.IsNullOrEmpty(messageProperties.CorrelationId))
            messageProperties = messageProperties with { CorrelationId = correlationIdGenerator.GetCorrelationId() };
        return new SerializedMessage(messageProperties, messageBody);
    }

    /// <inheritdoc />
    public SerializedMessage SerializeMessage<T>(T body, in MessageProperties properties)
        // versioning always derives the type stack from the runtime type, so the enveloped path is the only path
        => SerializeMessage(new Message<T>(body!, properties));

    /// <inheritdoc />
    public IMessage DeserializeMessage(in MessageProperties properties, in ReadOnlyMemory<byte> body)
    {
        var messageTypeProperty = MessageTypeProperty.ExtractFromProperties(properties, typeNameSerializer);
        var messageType = messageTypeProperty.GetMessageType();
        var descriptor = registry.GetOrAdd(messageType);
        var messageBody = body.IsEmpty ? null : descriptor.DeserializeBody(serializer, body);
        messageTypeProperty.AppendTo(properties);
        return descriptor.CreateMessage(messageBody, properties);
    }
}
