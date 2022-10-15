using System;
using System.Buffers;
using EasyNetQ.Internals;

namespace EasyNetQ.Serialization.SystemTextJson;

public sealed class SystemTextJsonSerializer : ISerializer
{
    private readonly System.Text.Json.JsonSerializerOptions serialiseOptions;
    private readonly System.Text.Json.JsonSerializerOptions deserializeOptions;

    public SystemTextJsonSerializer()
        : this(new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.General))
    {
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public SystemTextJsonSerializer(System.Text.Json.JsonSerializerOptions options)
    {
        serialiseOptions = new System.Text.Json.JsonSerializerOptions(options);
        deserializeOptions = new System.Text.Json.JsonSerializerOptions(options);
        deserializeOptions.Converters.Add(new SystemObjectNewtonsoftCompatibleConverter());
    }

    public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
    {
        var stream = new ArrayPooledMemoryStream();
        System.Text.Json.JsonSerializer.Serialize(stream, message, messageType, serialiseOptions);
        return stream;
    }

    public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
    {
        return System.Text.Json.JsonSerializer.Deserialize(bytes.Span, messageType, deserializeOptions)!;
    }
}
