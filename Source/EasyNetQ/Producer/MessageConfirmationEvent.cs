namespace EasyNetQ.Producer
{
    public class MessageConfirmationEvent
    {
        public ulong DeliveryTag { get; private set; }
        public bool Multiple { get; private set; }
        public bool IsNack { get; private set; }

        public static MessageConfirmationEvent Ack(ulong deliveryTag, bool multiple)
        {
            return new MessageConfirmationEvent
            {
                IsNack = false,
                DeliveryTag = deliveryTag,
                Multiple = multiple
            };
        }

        public static MessageConfirmationEvent Nack(ulong deliveryTag, bool multiple)
        {
            return new MessageConfirmationEvent
            {
                IsNack = true,
                DeliveryTag = deliveryTag,
                Multiple = multiple
            };
        }
    }
}