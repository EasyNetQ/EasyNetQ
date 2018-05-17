
namespace EasyNetQ
{
    public interface IMessageSerializationStrategy
    {
        SerializedMessage SerializeMessage(IMessage message);
        IMessage DeserializeMessage(MessageProperties properties, byte[] body);
    }

    public class SerializedMessage
    {
        public SerializedMessage(MessageProperties properties, byte[] body)
        {
            Properties = properties;
            Body = body;
        }

        public MessageProperties Properties { get; }
        public byte[] Body { get; }
    }     
}