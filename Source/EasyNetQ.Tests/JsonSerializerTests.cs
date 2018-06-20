using System.Collections.Generic;
using FluentAssertions;
using RabbitMQ.Client.Framing;
// ReSharper disable InconsistentNaming
using System;
using System.Text;
using Xunit;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class JsonSerializerTests
    {
        private ISerializer serializer;

        public JsonSerializerTests()
        {
            serializer = new JsonSerializer();
        }

        [Fact]
        public void Should_be_able_to_serialize_and_deserialize_a_message()
        {
            var message = new MyMessage {Text = "Hello World"};

            var binaryMessage = serializer.MessageToBytes(message);
            var deseralizedMessage = serializer.BytesToMessage<MyMessage>(binaryMessage);

            message.Text.Should().Be(deseralizedMessage.Text);
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
                    {"one", "header one"},
                    {"two", "header two"}
                }
            };

            var messageBasicProperties = new MessageProperties(originalProperties);
            var binaryMessage = serializer.MessageToBytes(messageBasicProperties);
            var deserializedMessageBasicProperties = serializer.BytesToMessage<MessageProperties>(binaryMessage);

            var newProperties = new BasicProperties();
            deserializedMessageBasicProperties.CopyTo(newProperties);

            Func<BasicProperties, string> getPropertiesString = p =>
            {
                var builder = new StringBuilder();
                p.AppendPropertyDebugStringTo(builder);
                return builder.ToString();
            };

            getPropertiesString(originalProperties).Should().Be(getPropertiesString(newProperties));
        }

        class A { }
        class B : A {}
        class PolyMessage
        {
            public A AorB { get; set; }
        }

        [Fact]
        public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties()
        {
            var bytes = serializer.MessageToBytes<PolyMessage>(new PolyMessage { AorB = new B() });

            var result = serializer.BytesToMessage<PolyMessage>(bytes);

            Assert.IsType<B>(result.AorB);
        }

        [Fact]
        public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties_when_using_TypeNameSerializer()
        {
            var bytes = serializer.MessageToBytes(new PolyMessage { AorB = new B() });
            var result = (PolyMessage)serializer.BytesToMessage(typeof(PolyMessage), bytes);

            Assert.IsType<B>(result.AorB);
        }
    }
}

// ReSharper restore InconsistentNaming