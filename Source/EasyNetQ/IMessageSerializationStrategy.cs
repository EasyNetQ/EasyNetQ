namespace EasyNetQ
{
    /// <summary>
    ///     The message serialization strategy
    /// </summary>
    public interface IMessageSerializationStrategy
    {
        /// <summary>
        ///     Serializes the message
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns></returns>
        SerializedMessage SerializeMessage(IMessage message);

        /// <summary>
        ///     Deserializes the message
        /// </summary>
        /// <param name="properties">The properties</param>
        /// <param name="body">The body</param>
        /// <returns></returns>
        IMessage DeserializeMessage(MessageProperties properties, byte[] body);
    }

    /// <summary>
    ///     Represents a serialized message
    /// </summary>
    public readonly struct SerializedMessage
    {
        /// <summary>
        ///     Creates SerializedMessage
        /// </summary>
        /// <param name="properties">The properties</param>
        /// <param name="body">The body</param>
        public SerializedMessage(MessageProperties properties, byte[] body)
        {
            Properties = properties;
            Body = body;
        }

        /// <summary>
        ///     Message properties
        /// </summary>
        public MessageProperties Properties { get; }

        /// <summary>
        ///     Message body
        /// </summary>
        public byte[] Body { get; }
    }
}
