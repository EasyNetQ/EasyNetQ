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
            var persistentAttribute = messageType.GetCustomAttributes(typeof (DeliveryModeAttribute), true).FirstOrDefault() as DeliveryModeAttribute;
            if (persistentAttribute != null)
                return persistentAttribute.IsPersistent;
            return connectionConfiguration.PersistentMessages;
        }
    }
}