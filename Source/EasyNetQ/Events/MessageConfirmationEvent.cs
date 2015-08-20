using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    public class MessageConfirmationEvent
    {
        public IModel Channel { get; private set; }
        public ulong DeliveryTag { get; private set; }
        public bool Multiple { get; private set; }
        public bool IsNack { get; private set; }

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