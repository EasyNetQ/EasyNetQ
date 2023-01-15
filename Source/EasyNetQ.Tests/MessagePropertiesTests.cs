namespace EasyNetQ.Tests;

public class MessagePropertiesTests
{
    [Fact]
    public void Should_copy_from_Rabbit_client_properties()
    {
        const string replyTo = "reply to";

        var originalProperties = new BasicProperties { ReplyTo = replyTo };
        var properties = new MessageProperties(originalProperties);

        properties.ReplyTo.Should().Be(replyTo);
    }
}
