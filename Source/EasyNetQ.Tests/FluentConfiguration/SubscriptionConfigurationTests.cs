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
            Assert.Null(configuration.QueueType);
        }

        [Fact]
        public void WithQueueType_default_is_classic()
        {
            var configuration = new SubscriptionConfiguration(99);

            configuration.WithQueueType();

            Assert.Equal(QueueType.Classic, configuration.QueueType);
        }

        [Theory]
        [InlineData(QueueType.Classic)]
        [InlineData(QueueType.Quorum)]
        public void WithQueueType_sets_correct_queueType(string queueType)
        {
            var configuration = new SubscriptionConfiguration(99);

            configuration.WithQueueType(queueType);

            Assert.Equal(queueType, configuration.QueueType);
        }
    }
}
