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

        public byte[] MessageToBytes(Type messageType, object message)
        {
            Preconditions.CheckNotNull(messageType, "messageType");
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, messageType, serializerSettings));
        }

        public object BytesToMessage(Type messageType, byte[] bytes)
        {
            Preconditions.CheckNotNull(messageType, "messageType");
            Preconditions.CheckNotNull(bytes, "bytes");
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), messageType, serializerSettings);
        }
    }
}
