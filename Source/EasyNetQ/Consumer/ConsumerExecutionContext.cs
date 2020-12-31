using System;

namespace EasyNetQ.Consumer
{
    /// <summary>
    ///     Represent context of an executing message
    /// </summary>
    public readonly struct ConsumerExecutionContext
    {
        /// <summary>
        ///     Message handler
        /// </summary>
        public MessageHandler Handler { get; }

        /// <summary>
        ///     Message received info
        /// </summary>
        public MessageReceivedInfo ReceivedInfo { get; }

        /// <summary>
        ///     Message properties
        /// </summary>
        public MessageProperties Properties { get; }

        /// <summary>
        ///     Message body
        /// </summary>
        public ReadOnlyMemory<byte> Body { get; }

        /// <summary>
        ///     Creates ConsumerExecutionContext
        /// </summary>
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
