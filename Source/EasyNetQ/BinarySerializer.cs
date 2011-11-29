using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace EasyNetQ
{
    public class BinarySerializer : ISerializer
    {
        public byte[] MessageToBytes<T>(T message)
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

        public T BytesToMessage<T>(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
            {
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}