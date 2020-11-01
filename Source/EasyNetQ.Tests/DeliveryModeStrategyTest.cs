using System;
using Xunit;

namespace EasyNetQ.Tests
{
    [DeliveryMode(true)]
    public class PersistentMessageWithDeliveryAttribute
    {
    }

    [DeliveryMode(false)]
    public class NotPersistentMessageWithDeliveryAttribute
    {
    }

    public class MessageWithoutDeliveryAttribute
    {
    }

    public class DeliveryModeStrategyTest
    {
        [Theory]
        [InlineData(typeof(PersistentMessageWithDeliveryAttribute), true)]
        [InlineData(typeof(NotPersistentMessageWithDeliveryAttribute), false)]
        [InlineData(typeof(MessageWithoutDeliveryAttribute), true)]
        public void TestWhenPersistentMessagesIsTrue(Type messageType, bool isPersistent)
        {
            var deliveryModeStrategy = new MessageDeliveryModeStrategy(new ConnectionConfiguration { PersistentMessages = true });
            Assert.Equal(isPersistent ? MessageDeliveryMode.Persistent : MessageDeliveryMode.NonPersistent, deliveryModeStrategy.GetDeliveryMode(messageType));
        }

        [Theory]
        [InlineData(typeof(PersistentMessageWithDeliveryAttribute), true)]
        [InlineData(typeof(NotPersistentMessageWithDeliveryAttribute), false)]
        [InlineData(typeof(MessageWithoutDeliveryAttribute), false)]
        public void TestWhenPersistentMessagesIsFalse(Type messageType, bool isPersistent)
        {
            var deliveryModeStrategy = new MessageDeliveryModeStrategy(new ConnectionConfiguration { PersistentMessages = false });
            Assert.Equal(isPersistent ? MessageDeliveryMode.Persistent : MessageDeliveryMode.NonPersistent, deliveryModeStrategy.GetDeliveryMode(messageType));
        }
    }
}
