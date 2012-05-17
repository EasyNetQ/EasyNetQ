// ReSharper disable InconsistentNaming

using NUnit.Framework;
using RabbitMQ.Client.Framing.v0_9_1;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class MessagePropertiesTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Should_copy_from_Rabbit_client_properties()
        {
            const string replyTo = "reply to";

            var properties = new MessageProperties();
            var originalProperties = new BasicProperties {ReplyTo = replyTo};

            properties.CopyFrom(originalProperties);

            properties.ReplyTo.ShouldEqual(replyTo);
        }

        [Test]
        public void Should_copy_to_rabbit_client_properties()
        {
            const string replyTo = "reply to";

            var properties = new MessageProperties { ReplyTo = replyTo };
            var destinationProperties = new BasicProperties();

            properties.CopyTo(destinationProperties);

            destinationProperties.ReplyTo.ShouldEqual(replyTo);
            destinationProperties.IsReplyToPresent().ShouldBeTrue();
            destinationProperties.IsMessageIdPresent().ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming