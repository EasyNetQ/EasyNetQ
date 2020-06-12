namespace EasyNetQ
{
    /// <summary>
    ///     Represents a publishing message
    /// </summary>
    public readonly struct ProducedMessage
    {
        /// <summary>
        ///    Creates ProducedMessage
        /// </summary>
        /// <param name="properties">The properties</param>
        /// <param name="body">The body</param>
        public ProducedMessage(MessageProperties properties, byte[] body)
        {
            Properties = properties;
            Body = body;
        }

        /// <summary>
        ///     Various message properties
        /// </summary>
        public MessageProperties Properties { get; }

        /// <summary>
        ///     Message body
        /// </summary>
        public byte[] Body { get; }
    }
}
