namespace EasyNetQ.Tests.FluentConfiguration;

public class SubscriptionConfigurationTests
{
    [Fact]
    public void Defaults_are_correct()
    {
        var configuration = new SubscriptionConfiguration(99);
        Assert.Empty(configuration.Topics);
        Assert.False(configuration.AutoDelete);
        Assert.Equal(0, configuration.Priority);
        Assert.Equal(99, configuration.PrefetchCount);
        Assert.False(configuration.IsExclusive);
        Assert.True(configuration.Durable);
        Assert.Null(configuration.QueueName);
        Assert.Null(configuration.QueueArguments);
    }

    [Fact]
    public void WithQueueType_default_is_classic()
    {
        var configuration = new SubscriptionConfiguration(99);
        configuration.WithQueueType();
        configuration.QueueArguments.Should().BeEquivalentTo(new Dictionary<string, object> { { Argument.QueueType, QueueType.Classic } });
    }

    [Theory]
    [InlineData(QueueType.Classic)]
    [InlineData(QueueType.Quorum)]
    public void WithQueueType_sets_correct_queueType(string queueType)
    {
        var configuration = new SubscriptionConfiguration(99);
        configuration.WithQueueType(queueType);
        configuration.QueueArguments.Should().BeEquivalentTo(new Dictionary<string, object> { { Argument.QueueType, queueType } });
    }
}
