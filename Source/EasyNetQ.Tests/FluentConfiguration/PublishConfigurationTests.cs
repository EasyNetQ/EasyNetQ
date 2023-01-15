namespace EasyNetQ.Tests.FluentConfiguration;

public class PublishConfigurationTests
{
    [Fact]
    public void Should_return_default_topic()
    {
        var configuration = new PublishConfiguration("default");
        Assert.Equal("default", configuration.Topic);
    }

    [Fact]
    public void Should_return_custom_topic()
    {
        var configuration = new PublishConfiguration("default") with { Topic = "custom" };

        Assert.Equal("custom", configuration.Topic);
    }
}
