namespace EasyNetQ.Interception
{
    public /*readonly - requires C# 7.2*/ struct RawMessage
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