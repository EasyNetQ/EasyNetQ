using FluentAssertions;
using RabbitMQ.Client;
// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace EasyNetQ.Tests
{
    public class JsonSerializerTests
    {
        private readonly ISerializer serializer;

        public JsonSerializerTests()
        {
            serializer = new JsonSerializer();
        }

        [Fact]
        public void Should_be_able_to_serialize_and_deserialize_a_default_message()
        {
            using var serializedMessage = serializer.MessageToBytes(typeof(MyMessage), default(MyMessage));
            var deserializedMessage = (MyMessage)serializer.BytesToMessage(typeof(MyMessage), serializedMessage.Memory);
            deserializedMessage.Should().BeNull();
        }

        [Fact]
        public void Should_be_able_to_serialize_and_deserialize_a_message()
        {
            var message = new MyMessage { Text = "Hello World" };

            using var serializedMessage = serializer.MessageToBytes(typeof(MyMessage), message);
            var deserializedMessage = (MyMessage)serializer.BytesToMessage(typeof(MyMessage), serializedMessage.Memory);

            message.Text.Should().Be(deserializedMessage.Text);
        }

        [Fact]
        public void Should_be_able_to_serialize_basic_properties()
        {
            var originalProperties = new BasicProperties
            {
                AppId = "some app id",
                ClusterId = "cluster id",
                ContentEncoding = "content encoding",
                //ContentType = "content type",
                CorrelationId = "correlation id",
                DeliveryMode = 4,
                Expiration = "expiration",
                MessageId = "message id",
                Priority = 1,
                ReplyTo = "abc",
                Timestamp = new AmqpTimestamp(123344044),
                Type = "Type",
                UserId = "user id",
                Headers = new Dictionary<string, object>
                {
                    { "one", "header one" },
                    { "two", "header two" }
                }
            };

            var messageBasicProperties = new MessageProperties(originalProperties);
            using var serializedMessage = serializer.MessageToBytes(typeof(MessageProperties), messageBasicProperties);
            var deserializedMessageBasicProperties = (MessageProperties)serializer.BytesToMessage(
                typeof(MessageProperties), serializedMessage.Memory
            );

            var newProperties = new BasicProperties();
            deserializedMessageBasicProperties.CopyTo(newProperties);

            originalProperties.Should().BeEquivalentTo(newProperties);
        }

        private class A { }
        private class B : A { }
        private class PolyMessage
        {
            public A AorB { get; set; }
        }

        [Fact]
        public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties()
        {
            using var serializedMessage = serializer.MessageToBytes(typeof(PolyMessage), new PolyMessage { AorB = new B() });
            var result = (PolyMessage)serializer.BytesToMessage(typeof(PolyMessage), serializedMessage.Memory);
            Assert.IsType<B>(result.AorB);
        }

        [Fact]
        public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties_when_using_TypeNameSerializer()
        {
            using var serializedMessage = serializer.MessageToBytes(typeof(PolyMessage), new PolyMessage { AorB = new B() });
            var result = (PolyMessage)serializer.BytesToMessage(typeof(PolyMessage), serializedMessage.Memory);
            Assert.IsType<B>(result.AorB);
        }
    }
}

// ReSharper restore InconsistentNaming
