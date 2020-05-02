using System;
using System.IO;
using System.Text;

namespace EasyNetQ
{
    public class JsonSerializer : ISerializer
    {
        private static readonly Newtonsoft.Json.JsonSerializerSettings DefaultSerializerSettings =
            new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
            };

        private const int DefaultBufferSize = 1024;

        private readonly Newtonsoft.Json.JsonSerializer serializer;

        public JsonSerializer() : this(DefaultSerializerSettings)
        {
        }

        public JsonSerializer(Newtonsoft.Json.JsonSerializerSettings serializerSettings)
        {
            serializer = Newtonsoft.Json.JsonSerializer.Create(serializerSettings);
        }

        public byte[] MessageToBytes(Type messageType, object message)
        {
            Preconditions.CheckNotNull(messageType, "messageType");

            using (var memoryStream = new MemoryStream(DefaultBufferSize))
            {
                using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, DefaultBufferSize, true))
                using (var writer = new Newtonsoft.Json.JsonTextWriter(streamWriter))
                    serializer.Serialize(writer, message, messageType);

                return memoryStream.ToArray();
            }
        }

        public object BytesToMessage(Type messageType, byte[] bytes)
        {
            Preconditions.CheckNotNull(messageType, "messageType");
            Preconditions.CheckNotNull(bytes, "bytes");

            using (var memoryStream = new MemoryStream(bytes, false))
            using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8, false, DefaultBufferSize, true))
            using (var reader = new Newtonsoft.Json.JsonTextReader(streamReader))
                return serializer.Deserialize(reader, messageType);
        }
    }
}
