using System;

namespace EasyNetQ
{
    public interface ISerializer
    {
        byte[] Serialize(object message);
        object Deserialize(Type messageType, byte[] bytes);
    }

    public static class SerializerExtensions
    {
        public static byte[] MessageToBytes<T>(this ISerializer serializer, T message)
        {
            return serializer.Serialize(message);
        }
        
        public static T BytesToMessage<T>(this ISerializer serializer, byte[] bytes)
        {
            var messageType = typeof (T);
            return (T) serializer.Deserialize(messageType, bytes);
        }
    }
}