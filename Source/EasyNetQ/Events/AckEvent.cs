using System;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a message is acked
    /// </summary>
    public readonly struct AckEvent
    {
        /// <summary>
        ///     Acked message received info
        /// </summary>
        public MessageReceivedInfo ReceivedInfo { get; }

        /// <summary>
        ///     Acked message properties
        /// </summary>
        public MessageProperties Properties { get; }

        /// <summary>
        ///     Acked message body
        /// </summary>
        public ReadOnlyMemory<byte> Body { get; }

        /// <summary>
        ///     Ack result of message
        /// </summary>
        public AckResult AckResult { get; }

        /// <summary>
        ///     Creates AckEvent
        /// </summary>
        public AckEvent(MessageReceivedInfo info, MessageProperties properties, in ReadOnlyMemory<byte> body, AckResult ackResult)
        {
            ReceivedInfo = info;
            Properties = properties;
            Body = body;
            AckResult = ackResult;
        }
    }


    /// <summary>
    ///     Represents various ack results
    /// </summary>
    public enum AckResult
    {
        /// <summary>
        ///     Message is acknowledged
        /// </summary>
        Ack,

        /// <summary>
        ///     Message is rejected
        /// </summary>
        Nack,

        /// <summary>
        ///     Message is failed to process
        /// </summary>
        Exception
    }
}
