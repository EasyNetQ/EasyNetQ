namespace EasyNetQ.Events
{
    public class ReturnedMessageEvent
    {
        public byte[] Body { get; }
        public MessageProperties Properties { get; }
        public MessageReturnedInfo Info { get; }
        
        public ReturnedMessageEvent(byte[] body, MessageProperties properties, MessageReturnedInfo info)
        {
            Body = body;
            Properties = properties;
            Info = info;
        }
    }
}