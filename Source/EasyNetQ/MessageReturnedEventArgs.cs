using System;

namespace EasyNetQ
{
    public class MessageReturnedEventArgs : EventArgs
    {
        public ReadOnlyMemory<byte> MessageBody { get; }
        public MessageProperties MessageProperties { get; }
        public MessageReturnedInfo MessageReturnedInfo { get; }

        public MessageReturnedEventArgs(ReadOnlyMemory<byte> messageBody, MessageProperties messageProperties, MessageReturnedInfo messageReturnedInfo)
        {
            Preconditions.CheckNotNull(messageProperties, "messageProperties");
            Preconditions.CheckNotNull(messageReturnedInfo, "messageReturnedInfo");

            MessageBody = messageBody;
            MessageProperties = messageProperties;
            MessageReturnedInfo = messageReturnedInfo;
        }
    }
}
