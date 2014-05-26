using System;

namespace EasyNetQ
{
    public interface IMessageSerializationStrategy
    {
        SerializedMessage SerializeMessage<T>(IMessage<T> message) where T : class;
        DeserializedMessage DeserializeMessage(MessageProperties properties, byte[] body);
    }

    public class SerializedMessage
    {
        public SerializedMessage(MessageProperties properties, byte[] messageBody)
        {
            Properties = properties;
            Body = messageBody;
        }

        public MessageProperties Properties { get; private set; }
        public byte[] Body { get; private set; }
    }

    public class DeserializedMessage
    {
        public DeserializedMessage(Type messageType, dynamic message)
        {
            MessageType = messageType;
            Message = message;
        }

        public Type MessageType { get; private set; }
        public dynamic Message { get; private set; }
    }
}