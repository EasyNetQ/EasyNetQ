using System.Collections.Generic;
using RabbitMQ.Client.Framing;
// ReSharper disable InconsistentNaming
using System;
using System.Text;
using NUnit.Framework;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class JsonSerializerTests
    {
        private ISerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new JsonSerializer(new TypeNameSerializer());
        }

        [Test]
        public void Should_be_able_to_serialize_and_deserialize_a_message()
        {
            var message = new MyMessage {Text = "Hello World"};

            var binaryMessage = serializer.MessageToBytes(message);
            var deseralizedMessage = serializer.BytesToMessage<MyMessage>(binaryMessage);

            message.Text.ShouldEqual(deseralizedMessage.Text);
        }

        [Test]
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

            getPropertiesString(originalProperties).ShouldEqual(getPropertiesString(newProperties));
        }

        class A { }
        class B : A {}
        class PolyMessage
        {
            public A AorB { get; set; }
        }

        [Test]
        public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties()
        {
            var bytes = serializer.MessageToBytes<PolyMessage>(new PolyMessage { AorB = new B() });

            var result = serializer.BytesToMessage<PolyMessage>(bytes);

            Assert.IsInstanceOf<B>(result.AorB);
        }

        [Test]
        public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties_when_using_TypeNameSerializer()
        {
            var typeName = new TypeNameSerializer().Serialize(typeof (PolyMessage));

            var bytes = serializer.MessageToBytes(new PolyMessage { AorB = new B() });
            var result = (PolyMessage)serializer.BytesToMessage(typeName, bytes);

            Assert.IsInstanceOf<B>(result.AorB);
        }
    }
}

// ReSharper restore InconsistentNaming