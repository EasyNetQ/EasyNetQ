using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a message is acked or nacked
    /// </summary>
    public class MessageConfirmationEvent
    {
        /// <summary>
        ///     The channel
        /// </summary>
        public IModel Channel { get; private set; }

        /// <summary>
        ///     Delivery tag of the message
        /// </summary>
        public ulong DeliveryTag { get; private set; }

        /// <summary>
        ///     True if a confirmation affects all previous messages
        /// </summary>
        public bool Multiple { get; private set; }

        /// <summary>
        ///     True if a message is rejected
        /// </summary>
        public bool IsNack { get; private set; }

        /// <summary>
        ///     Creates ack event
        /// </summary>
        /// <param name="channel">The channel</param>
        /// <param name="deliveryTag">The delivery tag</param>
        /// <param name="multiple">The multiple option</param>
        /// <returns></returns>
        public static MessageConfirmationEvent Ack(IModel channel, ulong deliveryTag, bool multiple)
        {
            return new MessageConfirmationEvent
            {
                Channel = channel,
                IsNack = false,
                DeliveryTag = deliveryTag,
                Multiple = multiple
            };
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
            return new MessageConfirmationEvent
            {
                Channel = channel,
                IsNack = true,
                DeliveryTag = deliveryTag,
                Multiple = multiple
            };
        }
    }
}
