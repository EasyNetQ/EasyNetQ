namespace EasyNetQ
{
    /// <summary>
    ///     Represents a consumed message
    /// </summary>
    public readonly struct ConsumedMessage
    {
        /// <summary>
        ///     Creates ConsumedMessage
        /// </summary>
        /// <param name="receivedInfo">The received info</param>
        /// <param name="properties">The properties</param>
        /// <param name="body">The body</param>
        public ConsumedMessage(MessageReceivedInfo receivedInfo, MessageProperties properties, byte[] body)
        {
            ReceivedInfo = receivedInfo;
            Properties = properties;
            Body = body;
        }

        /// <summary>
        ///     A received info associated with the message
        /// </summary>
        public MessageReceivedInfo ReceivedInfo { get; }

        /// <summary>
        ///     Various message properties
        /// </summary>
        public MessageProperties Properties { get; }

        /// <summary>
        ///     The message body
        /// </summary>
        public byte[] Body { get; }
    }
}
