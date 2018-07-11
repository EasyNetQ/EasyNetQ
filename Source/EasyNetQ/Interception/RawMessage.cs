namespace EasyNetQ.Interception
{
    public struct RawMessage
    {
        public RawMessage(MessageProperties properties, byte[] body)
        {
            Properties = properties;
            Body = body;
        }

        public MessageProperties Properties { get; }
        public byte[] Body { get; }
    }
}