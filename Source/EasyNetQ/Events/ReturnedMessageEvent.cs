using System;
using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a message is returned because it couldn't be routed
    /// </summary>
    public readonly struct ReturnedMessageEvent
    {
        /// <summary>
        ///     Creates ReturnedMessageEvent
        /// </summary>
        /// <param name="channel">The channel</param>
        /// <param name="body">The message body</param>
        /// <param name="properties">The message properties</param>
        /// <param name="info">The returned message info</param>
        public ReturnedMessageEvent(IModel channel, in ReadOnlyMemory<byte> body, MessageProperties properties, in MessageReturnedInfo info)
        {
            Channel = channel;
            Body = body;
            Properties = properties;
            Info = info;
        }

        /// <summary>
        ///     The channel
        /// </summary>
        public IModel Channel { get; }

        /// <summary>
        ///     Message body
        /// </summary>
        public ReadOnlyMemory<byte> Body { get; }

        /// <summary>
        ///     Message properties
        /// </summary>
        public MessageProperties Properties { get; }

        /// <summary>
        ///     Message returned info
        /// </summary>
        public MessageReturnedInfo Info { get; }
    }
}
