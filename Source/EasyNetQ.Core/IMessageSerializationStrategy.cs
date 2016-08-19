
namespace EasyNetQ
{
    public interface IMessageSerializationStrategy
    {
        SerializedMessage SerializeMessage(IMessage message);
        IMessage<T> DeserializeMessage<T>(MessageProperties properties, byte[] body) where T : class;
        IMessage DeserializeMessage(MessageProperties properties, byte[] body);
    }

    public class SerializedMessage
    {
        public SerializedMessage(MessageProperties properties, byte[] body)
        {
            Properties = properties;
            Body = body;
        }

        public MessageProperties Properties { get; private set; }
        public byte[] Body { get; private set; }
    }     
}