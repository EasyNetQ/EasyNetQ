using System;
using System.Buffers;
using System.IO;
using System.Text;
using EasyNetQ.Internals;

namespace EasyNetQ.Serialization.NewtonsoftJson;

/// <summary>
///     Serializer based on Newtonsoft.Json
/// </summary>
public sealed class NewtonsoftJsonSerializer : ISerializer
{
    private static readonly Encoding Encoding = new UTF8Encoding(false);
    private static readonly Newtonsoft.Json.JsonSerializerSettings DefaultSerializerSettings =
        new()
        {
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
        };

    private const int DefaultBufferSize = 1024;

    private readonly Newtonsoft.Json.JsonSerializer jsonSerializer;

    /// <inheritdoc />
    public NewtonsoftJsonSerializer() : this(DefaultSerializerSettings)
    {
    }

    /// <summary>
    ///     Creates JsonSerializer
    /// </summary>
    public NewtonsoftJsonSerializer(Newtonsoft.Json.JsonSerializerSettings settings)
    {
        jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(settings);
    }

    /// <inheritdoc />
    public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
    {
        Preconditions.CheckNotNull(messageType, nameof(messageType));

        var stream = new ArrayPooledMemoryStream();

        using var streamWriter = new StreamWriter(stream, Encoding, DefaultBufferSize, true);
        using var jsonWriter = new Newtonsoft.Json.JsonTextWriter(streamWriter)
        {
            Formatting = jsonSerializer.Formatting,
            ArrayPool = JsonSerializerArrayPool<char>.Instance
        };

        jsonSerializer.Serialize(jsonWriter, message, messageType);

        return stream;
    }

    /// <inheritdoc />
    public object BytesToMessage(Type messageType, in ReadOnlyMemory<byte> bytes)
    {
        Preconditions.CheckNotNull(messageType, nameof(messageType));

        using var memoryStream = new ReadOnlyMemoryStream(bytes);
        using var streamReader = new StreamReader(memoryStream, Encoding, false, DefaultBufferSize, true);
        using var reader = new Newtonsoft.Json.JsonTextReader(streamReader) { ArrayPool = JsonSerializerArrayPool<char>.Instance };
        return jsonSerializer.Deserialize(reader, messageType);
    }

    private class JsonSerializerArrayPool<T> : Newtonsoft.Json.IArrayPool<T>
    {
        public static JsonSerializerArrayPool<T> Instance { get; } = new();

        public T[] Rent(int minimumLength) => ArrayPool<T>.Shared.Rent(minimumLength);

        public void Return(T[] array) => ArrayPool<T>.Shared.Return(array);
    }
}
