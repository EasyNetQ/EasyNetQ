using System;

namespace EasyNetQ
{
    public interface IMessageDeliveryModeStrategy
    {
        bool IsPersistent(Type messageType);
    }

    public class MessageDeliveryModeStrategy : IMessageDeliveryModeStrategy
    {
        private readonly ConnectionConfiguration connectionConfiguration;

        public MessageDeliveryModeStrategy(ConnectionConfiguration connectionConfiguration)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            this.connectionConfiguration = connectionConfiguration;
        }

        public bool IsPersistent(Type messageType)
        {
            Preconditions.CheckNotNull(messageType, "messageType");
            var deliveryModeAttribute = messageType.GetAttribute<DeliveryModeAttribute>();
            return deliveryModeAttribute != null ? deliveryModeAttribute.IsPersistent : connectionConfiguration.PersistentMessages;
        }
    }
}