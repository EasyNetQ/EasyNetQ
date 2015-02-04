using System.Text;
using Newtonsoft.Json;

namespace EasyNetQ
{
    public class JsonSerializer : ISerializer
    {
        private readonly ITypeNameSerializer typeNameSerializer;

        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        public JsonSerializer(ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");
            this.typeNameSerializer = typeNameSerializer;
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

        public object BytesToMessage(string typeName, byte[] bytes)
        {
            Preconditions.CheckNotNull(typeName, "typeName");
            Preconditions.CheckNotNull(bytes, "bytes");
            var type = typeNameSerializer.DeSerialize(typeName);
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type, serializerSettings);
        }
    }
}