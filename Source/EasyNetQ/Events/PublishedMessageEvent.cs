namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a message is published
    /// </summary>
    public class PublishedMessageEvent
    {
        /// <summary>
        ///     Creates PublishedMessageEvent
        /// </summary>
        /// <param name="exchangeName">The exchange name</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="properties">The properties</param>
        /// <param name="body">The body</param>
        public PublishedMessageEvent(string exchangeName, string routingKey, MessageProperties properties, byte[] body)
        {
            ExchangeName = exchangeName;
            RoutingKey = routingKey;
            Properties = properties;
            Body = body;
        }

        /// <summary>
        ///     The exchange name
        /// </summary>
        public string ExchangeName { get; }

        /// <summary>
        ///     The routing key
        /// </summary>
        public string RoutingKey { get; }

        /// <summary>
        ///     The message properties
        /// </summary>
        public MessageProperties Properties { get; }

        /// <summary>
        ///     The message body
        /// </summary>
        public byte[] Body { get; }
    }
}
