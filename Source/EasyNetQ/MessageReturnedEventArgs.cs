using System;

namespace EasyNetQ
{
    public class MessageReturnedEventArgs : EventArgs
    {
        public ReadOnlyMemory<byte> MessageBody { get; }
        public MessageProperties MessageProperties { get; }
        public MessageReturnedInfo MessageReturnedInfo { get; }

        public MessageReturnedEventArgs(in ReadOnlyMemory<byte> messageBody, MessageProperties messageProperties, in MessageReturnedInfo messageReturnedInfo)
        {
            Preconditions.CheckNotNull(messageProperties, nameof(messageProperties));
            Preconditions.CheckNotNull(messageReturnedInfo, nameof(messageReturnedInfo));

            MessageBody = messageBody;
            MessageProperties = messageProperties;
            MessageReturnedInfo = messageReturnedInfo;
        }
    }
}
