using System;
using System.Buffers;
using System.IO;
using System.Text;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    /// <summary>
    ///     JsonSerializer based on Newtonsoft.Json
    /// </summary>
    public class JsonSerializer : ISerializer
    {
        private static readonly Encoding Encoding = new UTF8Encoding(false);
        private static readonly Newtonsoft.Json.JsonSerializerSettings DefaultSerializerSettings =
            new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
            };

        private const int DefaultBufferSize = 1024;

        private readonly Newtonsoft.Json.JsonSerializer jsonSerializer;

        /// <inheritdoc />
        public JsonSerializer() : this(DefaultSerializerSettings)
        {
        }

        /// <summary>
        ///     Creates JsonSerializer
        /// </summary>
        public JsonSerializer(Newtonsoft.Json.JsonSerializerSettings serializerSettings)
        {
            jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(serializerSettings);
        }

        /// <inheritdoc />
        public IMemoryOwner<byte> MessageToBytes(Type messageType, object message)
        {
            Preconditions.CheckNotNull(messageType, "messageType");

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
            Preconditions.CheckNotNull(messageType, "messageType");

            using var memoryStream = new ReadOnlyMemoryStream(bytes);
            using var streamReader = new StreamReader(memoryStream, Encoding, false, DefaultBufferSize, true);
            using var reader = new Newtonsoft.Json.JsonTextReader(streamReader) { ArrayPool = JsonSerializerArrayPool<char>.Instance };
            return jsonSerializer.Deserialize(reader, messageType);
        }

        private class JsonSerializerArrayPool<T> : Newtonsoft.Json.IArrayPool<T>
        {
            public static JsonSerializerArrayPool<T> Instance { get; } = new JsonSerializerArrayPool<T>();

            public T[] Rent(int minimumLength) => ArrayPool<T>.Shared.Rent(minimumLength);

            public void Return(T[] array) => ArrayPool<T>.Shared.Return(array);
        }
    }
}
