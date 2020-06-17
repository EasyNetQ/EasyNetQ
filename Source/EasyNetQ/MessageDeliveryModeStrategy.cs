using System;
using EasyNetQ.Internals;

namespace EasyNetQ
{
    public interface IMessageDeliveryModeStrategy
    {
        byte GetDeliveryMode(Type messageType);
    }

    public class MessageDeliveryModeStrategy : IMessageDeliveryModeStrategy
    {
        private readonly ConnectionConfiguration connectionConfiguration;

        public MessageDeliveryModeStrategy(ConnectionConfiguration connectionConfiguration)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            this.connectionConfiguration = connectionConfiguration;
        }

        /// <inheritdoc />
        public byte GetDeliveryMode(Type messageType)
        {
            Preconditions.CheckNotNull(messageType, "messageType");
            var deliveryModeAttribute = messageType.GetAttribute<DeliveryModeAttribute>();
            if (deliveryModeAttribute == null)
                return GetDeliveryModeInternal(connectionConfiguration.PersistentMessages);
            return GetDeliveryModeInternal(deliveryModeAttribute.IsPersistent);
        }

        private static byte GetDeliveryModeInternal(bool isPersistent)
        {
            return isPersistent ? MessageDeliveryMode.Persistent : MessageDeliveryMode.NonPersistent;
        }
    }
}
