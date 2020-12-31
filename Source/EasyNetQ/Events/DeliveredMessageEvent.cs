using System;

namespace EasyNetQ.Events
{
    public class DeliveredMessageEvent
    {
        public MessageReceivedInfo ReceivedInfo { get; }
        public MessageProperties Properties { get; }
        public ReadOnlyMemory<byte> Body { get; }

        public DeliveredMessageEvent(MessageReceivedInfo info, MessageProperties properties, ReadOnlyMemory<byte> body)
        {
            ReceivedInfo = info;
            Properties = properties;
            Body = body;
        }
    }
}
