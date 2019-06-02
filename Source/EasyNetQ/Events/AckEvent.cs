namespace EasyNetQ.Events
{
    public class AckEvent
    {
        public MessageReceivedInfo ReceivedInfo { get; }
        public MessageProperties Properties { get; }
        public byte[] Body { get; }
        public AckResult AckResult { get; }

        public AckEvent(MessageReceivedInfo info, MessageProperties properties, byte[] body, AckResult ackResult)
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
        Exception
    }
}
