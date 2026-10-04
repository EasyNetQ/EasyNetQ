using System.Buffers;

namespace EasyNetQ.Serialization;

/// <summary>
///     Bridges a legacy type-erased <see cref="ISerializer" /> (e.g. a user's custom serializer or the Newtonsoft
///     package) onto the generic <see cref="IMessageSerializer" /> contract
/// </summary>
public sealed class LegacyMessageSerializerAdapter : IMessageSerializer
{
    private readonly ISerializer serializer;

    /// <summary>
    ///     Creates the adapter over <paramref name="serializer" />
    /// </summary>
    public LegacyMessageSerializerAdapter(ISerializer serializer)
    {
        this.serializer = serializer;
    }

    /// <inheritdoc />
    public IMemoryOwner<byte> Serialize<T>(T body, MessageTypeDescriptor<T> descriptor)
        => serializer.MessageToBytes(descriptor.Type, body!);

    /// <inheritdoc />
    public T? Deserialize<T>(in ReadOnlyMemory<byte> body, MessageTypeDescriptor<T> descriptor)
        => (T?)serializer.BytesToMessage(descriptor.Type, body);
}
