using System.Text;
using Newtonsoft.Json;

namespace EasyNetQ
{
    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

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
    }
}