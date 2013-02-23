using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace EasyNetQ
{
    public class BinarySerializer : ISerializer
    {
        public byte[] Serialize(object message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            var formatter = new BinaryFormatter();
            byte[] messageBody;

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, message);
                messageBody = stream.GetBuffer();
            }
            return messageBody;
        }

        public object Deserialize(Type messageType, byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
            {
                return formatter.Deserialize(stream);
            }
        }
    }
}