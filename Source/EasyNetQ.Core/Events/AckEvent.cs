namespace EasyNetQ.Events
{
    public class AckEvent
    {
        public MessageReceivedInfo ReceivedInfo { get; private set; }
        public MessageProperties Properties { get; private set; }
        public byte[] Body { get; private set; }
        public AckResult AckResult { get; private set; }

        public AckEvent(MessageReceivedInfo info, MessageProperties properties, byte[] body , AckResult ackResult)
        {
            ReceivedInfo = info;
            Properties = properties;
            Body = body;
            AckResult = ackResult;
        }
    }

    public enum AckResult
    {
        Ack,
        Nack,
        Exception,
        Nothing
    }
}