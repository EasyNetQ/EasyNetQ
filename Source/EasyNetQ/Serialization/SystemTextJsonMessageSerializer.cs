using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using EasyNetQ.Internals;

namespace EasyNetQ.Serialization.SystemTextJson;

/// <summary>
///     The default <see cref="IMessageSerializer" />: System.Text.Json working through
///     <see cref="JsonTypeInfo{T}" /> contracts cached on the message type descriptor. With a
///     <see cref="JsonSerializerContext" /> supplied, serialization is reflection-free and AOT-safe.
/// </summary>
public sealed class SystemTextJsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions options;

    /// <summary>
    ///     Creates the serializer with the default options (general defaults + MessageProperties converter)
    /// </summary>
    public SystemTextJsonMessageSerializer()
        : this(new JsonSerializerOptions(JsonSerializerDefaults.General))
    {
    }

    /// <summary>
    ///     Creates the serializer with custom options
    /// </summary>
    public SystemTextJsonMessageSerializer(JsonSerializerOptions options)
    {
        this.options = new JsonSerializerOptions(options);
        this.options.Converters.Add(new MessagePropertiesConverter());
        this.options.MakeReadOnly(populateMissingResolver: true);
    }

    /// <summary>
    ///     Creates the serializer with a source-generated contract context (reflection-free, AOT-safe)
    /// </summary>
    public SystemTextJsonMessageSerializer(JsonSerializerContext context)
        : this((IJsonTypeInfoResolver)context)
    {
    }

    /// <summary>
    ///     Creates the serializer with an explicit contract resolver, e.g. several source-generated contexts
    ///     combined via <see cref="JsonTypeInfoResolver.Combine" /> (reflection-free, AOT-safe)
    /// </summary>
    public SystemTextJsonMessageSerializer(IJsonTypeInfoResolver resolver)
    {
        options = new JsonSerializerOptions(JsonSerializerDefaults.General) { TypeInfoResolver = resolver };
        options.Converters.Add(new MessagePropertiesConverter());
        options.MakeReadOnly(populateMissingResolver: false);
    }

    /// <inheritdoc />
    public IMemoryOwner<byte> Serialize<T>(T body, MessageTypeDescriptor<T> descriptor)
    {
        var stream = new ArrayPooledMemoryStream();
        JsonSerializer.Serialize(stream, body, GetTypeInfo(descriptor));
        return stream;
    }

    /// <inheritdoc />
    public T? Deserialize<T>(in ReadOnlyMemory<byte> body, MessageTypeDescriptor<T> descriptor)
        => JsonSerializer.Deserialize(body.Span, GetTypeInfo(descriptor));

    private JsonTypeInfo<T> GetTypeInfo<T>(MessageTypeDescriptor<T> descriptor)
    {
        // benign race: concurrent writers store the same JsonTypeInfo for this serializer's options
        if (descriptor.SerializerState is JsonTypeInfo<T> cached)
            return cached;

        var info = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        descriptor.SerializerState = info;
        return info;
    }
}
