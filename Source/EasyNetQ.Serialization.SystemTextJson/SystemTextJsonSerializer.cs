using System;
using System.Buffers;
using EasyNetQ.Internals;

namespace EasyNetQ.Serialization.SystemTextJson;

public sealed class SystemTextJsonSerializer : ISerializer
{
    private readonly System.Text.Json.JsonSerializerOptions options;

    public SystemTextJsonSerializer()
        : this(new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.General))
    {
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public SystemTextJsonSerializer(System.Text.Json.JsonSerializerOptions options)
    {
        this.options = new System.Text.Json.JsonSerializerOptions(options);
    }

    public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
    {
        var stream = new ArrayPooledMemoryStream();
        System.Text.Json.JsonSerializer.Serialize(stream, message, messageType, options);
        return stream;
    }

    public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
    {
        return System.Text.Json.JsonSerializer.Deserialize(bytes.Span, messageType, options);
    }
}
