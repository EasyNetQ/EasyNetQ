namespace EasyNetQ.Events
{
    public class ReturnedMessageEvent
    {
        public byte[] Body { get; set; }
        public MessageProperties Properties { get; set; }
        public MessageReturnedInfo Info { get; set; }
        
        public ReturnedMessageEvent(byte[] body, MessageProperties properties, MessageReturnedInfo info)
        {
            Body = body;
            Properties = properties;
            Info = info;
        }
    }
}