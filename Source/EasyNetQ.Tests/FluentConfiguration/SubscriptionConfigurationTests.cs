using Xunit;

namespace EasyNetQ.Tests.FluentConfiguration
{
    public class SubscriptionConfigurationTests
    {
        [Fact]
        public void Defaults_are_correct()
        {
            var configuration = new SubscriptionConfiguration(99);
            Assert.Equal(0, configuration.Topics.Count);
            Assert.False(configuration.AutoDelete);
            Assert.Equal(0, configuration.Priority);
            Assert.Equal(99, configuration.PrefetchCount);
            Assert.False(configuration.IsExclusive);
            Assert.True(configuration.Durable);
            Assert.Null(configuration.QueueName);
            Assert.Null(configuration.MaxLength);
            Assert.Null(configuration.MaxLengthBytes);
            Assert.Null(configuration.QueueMode);
        }
    }
}
