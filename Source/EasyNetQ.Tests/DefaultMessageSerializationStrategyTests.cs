// ReSharper disable InconsistentNaming

using NSubstitute;
using System;
using System.Text;
using Xunit;

namespace EasyNetQ.Tests
{
    public class DefaultMessageSerializationStrategyTests
    {
        [Fact]
        public void When_using_the_default_serialization_strategy_messages_are_correctly_serialized()
        {
            const string messageType = "MyMessageTypeName";
            var serializedMessageBody = Encoding.UTF8.GetBytes("Hello world!");
            const string correlationId = "CorrelationId";

            var message = new Message<MyMessage>(new MyMessage());
            var serializationStrategy = CreateSerializationStrategy(message, messageType, serializedMessageBody, correlationId);

            var serializedMessage = serializationStrategy.SerializeMessage(message);

            AssertMessageSerializedCorrectly(serializedMessage, serializedMessageBody, messageType, correlationId);
        }

        [Fact]
        public void When_serializing_a_message_with_a_correlation_id_it_is_not_overwritten()
        {
            const string messageType = "MyMessageTypeName";
            var serializedMessageBody = Encoding.UTF8.GetBytes("Hello world!");
            const string correlationId = "CorrelationId";

            var message = new Message<MyMessage>(new MyMessage())
                {
                    Properties = { CorrelationId = correlationId }
                };
            var serializationStrategy = CreateSerializationStrategy(message, messageType, serializedMessageBody, "SomeOtherCorrelationId");

            var serializedMessage = serializationStrategy.SerializeMessage(message);

            AssertMessageSerializedCorrectly(serializedMessage, serializedMessageBody, messageType, correlationId);
        }

        [Fact]
        public void When_using_the_default_serialization_strategy_messages_are_correctly_deserialized()
        {
            const string messageType = "MyMessageTypeName";
            const string messageContent = "Hello world!";
            var serializedMessageBody = Encoding.UTF8.GetBytes(messageContent);
            const string correlationId = "CorrelationId";

            var message = new Message<MyMessage>(new MyMessage { Text = messageContent })
            {
                Properties =
                    {
                        Type = messageType,
                        CorrelationId = correlationId,
                        UserId = "Bob"
                    },
            };
            var serializationStrategy = CreateDeserializationStrategy(message, serializedMessageBody, correlationId);

            var deserializedMessage = serializationStrategy.DeserializeMessage(message.Properties, serializedMessageBody);

            AssertMessageDeserializedCorrectly((Message<MyMessage>)deserializedMessage, messageContent, typeof(MyMessage), message.Properties.ToString());
        }

        [Fact]
        public void When_using_the_default_serialization_strategy_messages_are_correctly_round_tripped()
        {
            var typeNameSerializer = new DefaultTypeNameSerializer();
            var serializer = new JsonSerializer();
            const string correlationId = "CorrelationId";

            var serializationStrategy = new DefaultMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(correlationId));

            var messageBody = new MyMessage { Text = "Hello world!" };
            var message = new Message<MyMessage>(messageBody);
            var serializedMessage = serializationStrategy.SerializeMessage(message);
            var deserializedMessage = serializationStrategy.DeserializeMessage(serializedMessage.Properties, serializedMessage.Body);

            Assert.Equal(deserializedMessage.MessageType, message.Body.GetType());
            Assert.Equal(((Message<MyMessage>)deserializedMessage).Body.Text, message.Body.Text);
        }

        [Fact]
        public void When_using_the_default_serialization_strategy_messages_are_correctly_round_tripped_when_null()
        {
            var typeNameSerializer = new DefaultTypeNameSerializer();
            var serializer = new JsonSerializer();
            const string correlationId = "CorrelationId";

            var serializationStrategy = new DefaultMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(correlationId));

            var message = new Message<MyMessage>();
            var serializedMessage = serializationStrategy.SerializeMessage(message);
            var deserializedMessage = serializationStrategy.DeserializeMessage(serializedMessage.Properties, serializedMessage.Body);

            Assert.Equal(deserializedMessage.MessageType, message.MessageType);
            Assert.Null(((Message<MyMessage>)deserializedMessage).Body);
        }

        private void AssertMessageSerializedCorrectly(SerializedMessage message, byte[] expectedBody, string expectedMessageType, string expectedCorrelationId)
        {
            Assert.Equal(message.Body, expectedBody); //, "Serialized message body does not match expected value");
            Assert.Equal(message.Properties.Type, expectedMessageType); //, "Serialized message type does not match expected value");
            Assert.Equal(message.Properties.CorrelationId, expectedCorrelationId); //, "Serialized message correlation id does not match expected value");
        }

        private void AssertMessageDeserializedCorrectly(IMessage<MyMessage> message, string expectedBodyText, Type expectedMessageType, string expectedMessageProperties)
        {
            Assert.Equal(message.Body.Text, expectedBodyText); //, "Deserialized message body text does not match expected value");
            Assert.Equal(message.MessageType, expectedMessageType); //, "Deserialized message type does not match expected value");
            Assert.Equal(message.Properties.ToString(), expectedMessageProperties); //, "Deserialized message properties do not match expected value");
        }

        private static DefaultMessageSerializationStrategy CreateSerializationStrategy(IMessage<MyMessage> message, string messageType, byte[] messageBody, string correlationId)
        {
            var typeNameSerializer = Substitute.For<ITypeNameSerializer>();
            typeNameSerializer.Serialize(message.MessageType).Returns(messageType);

            var serializer = Substitute.For<ISerializer>();
            serializer.MessageToBytes(message.MessageType, message.GetBody()).Returns(messageBody);

            return new DefaultMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(correlationId));
        }

        private static DefaultMessageSerializationStrategy CreateDeserializationStrategy(IMessage<MyMessage> message, byte[] messageBody, string correlationId)
        {
            var typeNameSerializer = Substitute.For<ITypeNameSerializer>();
            typeNameSerializer.DeSerialize(message.Properties.Type).Returns(message.Body.GetType());

            var serializer = Substitute.For<ISerializer>();
            serializer.BytesToMessage(message.Body.GetType(), messageBody).Returns(message.Body);

            return new DefaultMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(correlationId));
        }
    }
}
