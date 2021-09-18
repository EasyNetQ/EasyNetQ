using System;
using EasyNetQ.Topology;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a message is published
    /// </summary>
    public readonly struct PublishedMessageEvent
    {
        /// <summary>
        ///     Creates PublishedMessageEvent
        /// </summary>
        /// <param name="exchange">The exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="properties">The properties</param>
        /// <param name="body">The body</param>
        public PublishedMessageEvent(in Exchange exchange, string routingKey, MessageProperties properties, in ReadOnlyMemory<byte> body)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
            Properties = properties;
            Body = body;
        }

        /// <summary>
        ///     The exchange name
        /// </summary>
        public Exchange Exchange { get; }

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
        public ReadOnlyMemory<byte> Body { get; }
    }
}
