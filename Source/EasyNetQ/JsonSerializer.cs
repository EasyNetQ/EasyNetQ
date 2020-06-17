using System;
using System.IO;
using System.Text;

namespace EasyNetQ
{
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

        public JsonSerializer() : this(DefaultSerializerSettings)
        {
        }

        public JsonSerializer(Newtonsoft.Json.JsonSerializerSettings serializerSettings)
        {
            jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(serializerSettings);
        }

        /// <inheritdoc />
        public byte[] MessageToBytes(Type messageType, object message)
        {
            Preconditions.CheckNotNull(messageType, "messageType");

            using var memoryStream = new MemoryStream(DefaultBufferSize);
            using (var streamWriter = new StreamWriter(memoryStream, Encoding, DefaultBufferSize, true))
            using (var jsonWriter = new Newtonsoft.Json.JsonTextWriter(streamWriter))
            {
                jsonWriter.Formatting = jsonSerializer.Formatting;
                jsonSerializer.Serialize(jsonWriter, message, messageType);
            }

            return memoryStream.ToArray();
        }

        /// <inheritdoc />
        public object BytesToMessage(Type messageType, byte[] bytes)
        {
            Preconditions.CheckNotNull(messageType, "messageType");
            Preconditions.CheckNotNull(bytes, "bytes");

            using var memoryStream = new MemoryStream(bytes, false);
            using var streamReader = new StreamReader(memoryStream, Encoding, false, DefaultBufferSize, true);
            using var reader = new Newtonsoft.Json.JsonTextReader(streamReader);
            return jsonSerializer.Deserialize(reader, messageType);
        }
    }
}
