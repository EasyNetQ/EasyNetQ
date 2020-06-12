using System;

namespace EasyNetQ
{
    public class MessageReturnedEventArgs : EventArgs
    {
        public byte[] MessageBody { get; }
        public MessageProperties MessageProperties { get; }
        public MessageReturnedInfo MessageReturnedInfo { get; }

        public MessageReturnedEventArgs(byte[] messageBody, MessageProperties messageProperties, MessageReturnedInfo messageReturnedInfo)
        {
            Preconditions.CheckNotNull(messageBody, "messageBody");
            Preconditions.CheckNotNull(messageProperties, "messageProperties");
            Preconditions.CheckNotNull(messageReturnedInfo, "messageReturnedInfo");

            MessageBody = messageBody;
            MessageProperties = messageProperties;
            MessageReturnedInfo = messageReturnedInfo;
        }
    }
}
