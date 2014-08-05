using System;
using System.Linq;

namespace EasyNetQ
{
    public interface IMessageDeliveryModeStrategy
    {
        bool IsPersistent(Type messageType);
    }

    public class MessageDeliveryModeStrategy : IMessageDeliveryModeStrategy
    {
        private readonly IConnectionConfiguration connectionConfiguration;

        public MessageDeliveryModeStrategy(IConnectionConfiguration connectionConfiguration)
        {
            this.connectionConfiguration = connectionConfiguration;
        }

        public bool IsPersistent(Type messageType)
        {
            Preconditions.CheckNotNull(messageType, "messageType");
            var deliveryModeAttribute = messageType.GetAttributes<DeliveryModeAttribute>().FirstOrDefault();
            if (deliveryModeAttribute != null)
                return deliveryModeAttribute.IsPersistent;
            return connectionConfiguration.PersistentMessages;
        }
    }
}