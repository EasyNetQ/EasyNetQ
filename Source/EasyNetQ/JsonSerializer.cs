using System;
using System.Text;
using Newtonsoft.Json;

namespace EasyNetQ
{
    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings serializerSettings;

        public JsonSerializer()
        {
            serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
        }

        public JsonSerializer(JsonSerializerSettings serializerSettings)
        {
            this.serializerSettings = serializerSettings;
        }

        public byte[] MessageToBytes<T>(T message) where T : class
        {
            Preconditions.CheckNotNull(message, "message");
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, serializerSettings));
        }

        public T BytesToMessage<T>(byte[] bytes)
        {
            Preconditions.CheckNotNull(bytes, "bytes");
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), serializerSettings);
        }

        public object BytesToMessage(Type type, byte[] bytes)
        {
            Preconditions.CheckNotNull(type, "type");
            Preconditions.CheckNotNull(bytes, "bytes");
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type, serializerSettings);
        }
    }
}