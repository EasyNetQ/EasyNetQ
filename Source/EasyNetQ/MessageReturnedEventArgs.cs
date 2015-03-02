using System;

namespace EasyNetQ
{
    public class MessageReturnedEventArgs : EventArgs
    {
        public byte[] MessageBody { get; private set; }
        public MessageProperties MessageProperties { get; private set; }
        public MessageReturnedInfo MessageReturnedInfo { get; private set; }

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