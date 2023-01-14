using System.Reflection;

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

    [Fact]
    public void Should_set_ConfirmationIdHeader()
    {
        var properties = new MessageProperties();
        properties.SetConfirmationId(12345UL);
        properties.Headers.Should().ContainKey(MessagePropertiesExtensions.ConfirmationIdHeader);
        properties.Headers[MessagePropertiesExtensions.ConfirmationIdHeader].Should().BeOfType<byte[]>();
        properties.Headers[MessagePropertiesExtensions.ConfirmationIdHeader].Should().BeEquivalentTo(new byte[] { (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5' });
    }
}
