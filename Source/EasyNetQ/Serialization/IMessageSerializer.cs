using System.Buffers;

namespace EasyNetQ;

/// <summary>
///     Serializes message bodies. Generic end to end: implementations work with <typeparamref name="T" /> directly
///     (letting e.g. System.Text.Json use its source-generated, AOT-safe contracts) and may cache per-type state in
///     <see cref="MessageTypeDescriptor.SerializerState" />. This replaces the type-erased <see cref="ISerializer" />;
///     a registered legacy <see cref="ISerializer" /> is still honoured through an adapter.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    ///     Serializes <paramref name="body" /> into pooled memory; the caller disposes the returned owner after the
    ///     transport has copied the bytes
    /// </summary>
    IMemoryOwner<byte> Serialize<T>(T body, MessageTypeDescriptor<T> descriptor);

    /// <summary>
    ///     Deserializes a body of type <typeparamref name="T" />
    /// </summary>
    T? Deserialize<T>(in ReadOnlyMemory<byte> body, MessageTypeDescriptor<T> descriptor);
}
