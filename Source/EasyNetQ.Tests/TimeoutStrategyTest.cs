using System;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TimeoutSeconds(90)]
    public class MessageWithTimeoutAttribute
    {
    }


    public class MessageWithoutTimeoutAttribute
    {
    }

    [TestFixture]
    public class TimeoutStrategyTest
    {
        [Test]
        public void TestWhenMessagetWithAttribute()
        {
            var timeoutStrategy = new TimeoutStrategy(new ConnectionConfiguration {Timeout = 10});
            Assert.AreEqual(90, timeoutStrategy.GetTimeoutSeconds(typeof(MessageWithTimeoutAttribute)));
        }

        [Test]
        public void TestWhenPersistentMessagesIsFalse()
        {
            var timeoutStrategy = new TimeoutStrategy(new ConnectionConfiguration { Timeout = 10 });
            Assert.AreEqual(10, timeoutStrategy.GetTimeoutSeconds(typeof(MessageWithoutTimeoutAttribute)));
        }
    }
}
