namespace EasyNetQ.Events
{
    public class ReturnedMessageEvent
    {
        public byte[] Body { get; private set; }
        public MessageProperties Properties { get; private set; }
        public MessageReturnedInfo Info { get; private set; }
        
        public ReturnedMessageEvent(byte[] body, MessageProperties properties, MessageReturnedInfo info)
        {
            Body = body;
            Properties = properties;
            Info = info;
        }
    }
}