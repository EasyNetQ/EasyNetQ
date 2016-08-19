namespace EasyNetQ.Interception
{
    public class RawMessage
    {
        public RawMessage(MessageProperties properties, byte[] body)
        {
            Properties = properties;
            Body = body;
        }

        public MessageProperties Properties { get; private set; }
        public byte[] Body { get; private set; }
    }
}