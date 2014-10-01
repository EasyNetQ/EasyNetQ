namespace EasyNetQ.Events
{
    public class DeliveredMessageEvent
    {
        public MessageReceivedInfo ReceivedInfo { get; private set; }
        public MessageProperties Properties { get; private set; }
        public byte[] Body { get; private set; }
     
        public DeliveredMessageEvent(MessageReceivedInfo info, MessageProperties properties, byte[] body)
        {
            ReceivedInfo = info;
            Properties = properties;
            Body = body;
        }
    }
}