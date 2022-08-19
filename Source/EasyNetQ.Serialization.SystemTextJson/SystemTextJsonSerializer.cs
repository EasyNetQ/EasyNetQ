using System;
using System.Buffers;
using System.Text.Json;
using EasyNetQ.Internals;

namespace EasyNetQ.Serialization.SystemTextJson;

public class SystemTextJsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions options;

    public SystemTextJsonSerializer() : this(new JsonSerializerOptions(JsonSerializerDefaults.General))
    {
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public SystemTextJsonSerializer(JsonSerializerOptions options)
    {
        this.options = new JsonSerializerOptions(options);
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
