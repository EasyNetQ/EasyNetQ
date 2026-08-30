using System.Buffers;

namespace EasyNetQ;

/// <summary>
///     Everything EasyNetQ knows about a message type: its CLR type, its wire type name (the AMQP "type" property)
///     and cached serializer state. Descriptors are created once per type by the <see cref="IMessageTypeRegistry" />
///     and shared; all typed work (serialize, deserialize, envelope creation) flows through the generic subclass so
///     no reflection is needed once a descriptor exists.
/// </summary>
public abstract class MessageTypeDescriptor
{
    private protected MessageTypeDescriptor(Type type, string wireName)
    {
        Type = type;
        WireName = wireName;
        DisplayName = type.FullName ?? type.Name;
    }

    /// <summary>
    ///     The CLR message type
    /// </summary>
    public Type Type { get; }

    /// <summary>
    ///     The wire type name written to and matched against the message's "type" property
    /// </summary>
    public string WireName { get; }

    /// <summary>
    ///     A stable, human-readable name for diagnostics (telemetry dimensions, log fields)
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Serializer-owned cache slot (e.g. a JsonTypeInfo). Races are benign: writers must store values that are
    ///     equivalent for the same serializer instance.
    /// </summary>
    public object? SerializerState { get; set; }

    /// <summary>
    ///     Serializes <paramref name="body" /> (which must be an instance of <see cref="Type" />)
    /// </summary>
    public abstract IMemoryOwner<byte> SerializeBody(IMessageSerializer serializer, object body);

    /// <summary>
    ///     Deserializes a body of this type
    /// </summary>
    public abstract object? DeserializeBody(IMessageSerializer serializer, in ReadOnlyMemory<byte> body);

    /// <summary>
    ///     Creates the typed <see cref="IMessage" /> envelope for <paramref name="body" /> (replaces the
    ///     expression-compiled MessageFactory)
    /// </summary>
    public abstract IMessage CreateMessage(object? body, in MessageProperties properties);
}

/// <summary>
///     The typed half of <see cref="MessageTypeDescriptor" />: closed over the message type so serializers can work
///     with <typeparamref name="T" /> directly (no boxing on the typed paths, no reflection anywhere)
/// </summary>
public sealed class MessageTypeDescriptor<T> : MessageTypeDescriptor
{
    internal MessageTypeDescriptor(string wireName) : base(typeof(T), wireName)
    {
    }

    /// <inheritdoc />
    public override IMemoryOwner<byte> SerializeBody(IMessageSerializer serializer, object body)
        => serializer.Serialize((T)body, this);

    /// <inheritdoc />
    public override object? DeserializeBody(IMessageSerializer serializer, in ReadOnlyMemory<byte> body)
        => serializer.Deserialize<T>(body, this);

    /// <summary>
    ///     Deserializes a body of this type without boxing
    /// </summary>
    public T? Deserialize(IMessageSerializer serializer, in ReadOnlyMemory<byte> body)
        => serializer.Deserialize<T>(body, this);

    /// <inheritdoc />
    public override IMessage CreateMessage(object? body, in MessageProperties properties)
        => new Message<T>((T)body!, properties);
}
