using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a message is acked or nacked
    /// </summary>
    public readonly struct MessageConfirmationEvent
    {
        /// <summary>
        ///     The channel
        /// </summary>
        public IModel Channel { get; }

        /// <summary>
        ///     Delivery tag of the message
        /// </summary>
        public ulong DeliveryTag { get; }

        /// <summary>
        ///     True if a confirmation affects all previous messages
        /// </summary>
        public bool Multiple { get; }

        /// <summary>
        ///     True if a message is rejected
        /// </summary>
        public bool IsNack { get; }

        /// <summary>
        ///     Creates ack event
        /// </summary>
        /// <param name="channel">The channel</param>
        /// <param name="deliveryTag">The delivery tag</param>
        /// <param name="multiple">The multiple option</param>
        /// <returns></returns>
        public static MessageConfirmationEvent Ack(IModel channel, ulong deliveryTag, bool multiple)
        {
            return new MessageConfirmationEvent(channel, deliveryTag, multiple, false);
        }

        /// <summary>
        ///     Creates nack event
        /// </summary>
        /// <param name="channel">The channel</param>
        /// <param name="deliveryTag">The delivery tag</param>
        /// <param name="multiple">The multiple option</param>
        /// <returns></returns>
        public static MessageConfirmationEvent Nack(IModel channel, ulong deliveryTag, bool multiple)
        {
            return new MessageConfirmationEvent(channel, deliveryTag, multiple, true);
        }

        private MessageConfirmationEvent(IModel channel, ulong deliveryTag, bool multiple, bool isNack)
        {
            Channel = channel;
            DeliveryTag = deliveryTag;
            Multiple = multiple;
            IsNack = isNack;
        }
    }
}
