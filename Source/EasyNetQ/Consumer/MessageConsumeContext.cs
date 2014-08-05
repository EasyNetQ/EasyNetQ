namespace EasyNetQ.Consumer
{
    public class MessageConsumeContext
    {
        public byte[] Message { get; private set; }
        public MessageProperties Properties { get; private set; }
        public MessageReceivedInfo Info { get; private set; }

        public MessageConsumeContext(byte[] message, MessageProperties properties, MessageReceivedInfo info)
        {
            Message = message;
            Properties = properties;
            Info = info;
        }
    }
}