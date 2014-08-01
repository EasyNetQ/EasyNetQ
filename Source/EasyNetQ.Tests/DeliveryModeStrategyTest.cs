using System;
using NUnit.Framework;

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

    [TestFixture]
    public class DeliveryModeStrategyTest
    {
        [Test]
        [TestCase(typeof(PersistentMessageWithDeliveryAttribute), true)]
        [TestCase(typeof(NotPersistentMessageWithDeliveryAttribute), false)]
        [TestCase(typeof(MessageWithoutDeliveryAttribute), true)]
        public void TestWhenPersistentMessagesIsTrue(Type messageType, bool isPersistent)
        {
            var deliveryModeStrategy = new MessageDeliveryModeStrategy(new ConnectionConfiguration {PersistentMessages = true});
            Assert.AreEqual(isPersistent, deliveryModeStrategy.IsPersistent(messageType));
        }

        [Test]
        [TestCase(typeof(PersistentMessageWithDeliveryAttribute), true)]
        [TestCase(typeof(NotPersistentMessageWithDeliveryAttribute), false)]
        [TestCase(typeof(MessageWithoutDeliveryAttribute), false)]
        public void TestWhenPersistentMessagesIsFalse(Type messageType, bool isPersistent)
        {
            var deliveryModeStrategy = new MessageDeliveryModeStrategy(new ConnectionConfiguration { PersistentMessages = false });
            Assert.AreEqual(isPersistent, deliveryModeStrategy.IsPersistent(messageType));
        }
    }
}
