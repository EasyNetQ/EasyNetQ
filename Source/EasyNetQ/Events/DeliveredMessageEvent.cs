namespace EasyNetQ.Events
{
    public struct DeliveredMessageEvent
    {
        public MessageReceivedInfo ReceivedInfo { get; }
        public MessageProperties Properties { get; }
        public byte[] Body { get; }

        public DeliveredMessageEvent(MessageReceivedInfo info, MessageProperties properties, byte[] body)
        {
            ReceivedInfo = info;
            Properties = properties;
            Body = body;
        }
    }
}
