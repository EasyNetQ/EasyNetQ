using System.Buffers;

namespace EasyNetQ;

/// <summary>
///     Represents a strategy of serialization/deserialization of messages
/// </summary>
public interface IMessageSerializationStrategy
{
    /// <summary>
    ///     Serializes an enveloped message (type-erased; used by <see cref="IAdvancedBus.PublishAsync(string,string,bool?,bool?,IMessage,System.Threading.CancellationToken)" />
    ///     and by polymorphic publishes where the body's runtime type differs from the static type)
    /// </summary>
    SerializedMessage SerializeMessage(IMessage message);

    /// <summary>
    ///     Serializes a body whose runtime type is its static type — the common, reflection-free fast path
    /// </summary>
    SerializedMessage SerializeMessage<T>(T body, in MessageProperties properties);

    /// <summary>
    ///     Deserializes a message into its typed <see cref="IMessage" /> envelope
    /// </summary>
    IMessage DeserializeMessage(in MessageProperties properties, in ReadOnlyMemory<byte> body);
}

/// <summary>
///     Represents a serialized message
/// </summary>
public readonly struct SerializedMessage : IDisposable
{
    private readonly IDisposable owner;

    /// <summary>
    ///     Creates SerializedMessage
    /// </summary>
    /// <param name="properties">The properties</param>
    /// <param name="body">The body</param>
    public SerializedMessage(in MessageProperties properties, IMemoryOwner<byte> body)
    {
        Properties = properties;
        Body = body.Memory;
        owner = body;
    }

    /// <summary>
    ///     Message properties
    /// </summary>
    public MessageProperties Properties { get; }

    /// <summary>
    ///     Message body
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <inheritdoc />
    public void Dispose() => owner?.Dispose();
}
