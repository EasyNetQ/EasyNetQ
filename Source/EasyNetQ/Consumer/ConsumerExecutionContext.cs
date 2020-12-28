using System;

namespace EasyNetQ.Consumer
{
    /// <summary>
    ///     Represent context of an executing message
    /// </summary>
    public readonly struct ConsumerExecutionContext
    {
        public MessageHandler Handler { get; }
        public MessageReceivedInfo ReceivedInfo { get; }
        public MessageProperties Properties { get; }
        public ReadOnlyMemory<byte> Body { get; }

        public ConsumerExecutionContext(
            MessageHandler handler,
            MessageReceivedInfo receivedInfo,
            MessageProperties properties,
            ReadOnlyMemory<byte> body
        )
        {
            Preconditions.CheckNotNull(handler, nameof(handler));
            Preconditions.CheckNotNull(receivedInfo, nameof(receivedInfo));
            Preconditions.CheckNotNull(properties, nameof(properties));

            Handler = handler;
            ReceivedInfo = receivedInfo;
            Properties = properties;
            Body = body;
        }
    }
}
