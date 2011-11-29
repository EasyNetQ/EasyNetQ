using System.Text;
using Newtonsoft.Json;

namespace EasyNetQ
{
    public class JsonSerializer : ISerializer
    {
        public byte[] MessageToBytes<T>(T message)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        }

        public T BytesToMessage<T>(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }
    }
}