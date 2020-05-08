using Xunit;

namespace EasyNetQ.Tests
{
    [TimeoutSeconds(90)]
    public class MessageWithTimeoutAttribute
    {
    }

    public class MessageWithoutTimeoutAttribute
    {
    }

    public class TimeoutStrategyTest
    {
        [Fact]
        public void TestWhenMessagetWithAttribute()
        {
            var timeoutStrategy = new TimeoutStrategy(new ConnectionConfiguration { Timeout = 10 });
            Assert.Equal(90UL, timeoutStrategy.GetTimeoutSeconds(typeof(MessageWithTimeoutAttribute)));
        }

        [Fact]
        public void TestWhenPersistentMessagesIsFalse()
        {
            var timeoutStrategy = new TimeoutStrategy(new ConnectionConfiguration { Timeout = 10 });
            Assert.Equal(10UL, timeoutStrategy.GetTimeoutSeconds(typeof(MessageWithoutTimeoutAttribute)));
        }
    }
}
