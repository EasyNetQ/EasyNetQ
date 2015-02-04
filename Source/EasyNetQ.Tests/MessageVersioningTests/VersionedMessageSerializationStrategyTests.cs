// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ.MessageVersioning;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    [TestFixture]
    public class VersionedMessageSerializationStrategyTests
    {
        private const string AlternativeMessageTypesHeaderKey = "Alternative-Message-Types";

        [Test]
        public void When_using_the_versioned_serialization_strategy_messages_are_correctly_serialized()
        {
            const string messageType = "MyMessageTypeName";
            var serializedMessageBody = Encoding.UTF8.GetBytes("Hello world!");
            const string correlationId = "CorrelationId";
            var messageTypes = new Dictionary<string, Type> { { messageType, typeof(MyMessage) } };

            var message = new Message<MyMessage>(new MyMessage());
            var serializationStrategy = CreateSerializationStrategy(message, messageTypes, serializedMessageBody, correlationId);

            var serializedMessage = serializationStrategy.SerializeMessage(message);

            AssertMessageSerializedCorrectly(serializedMessage, serializedMessageBody, p => AssertDefaultMessagePropertiesCorrect(p, messageType, correlationId));
        }

        [Test]
        public void When_serializing_a_message_with_a_correlationid_it_is_not_overwritten()
        {
            const string messageType = "MyMessageTypeName";
            var serializedMessageBody = Encoding.UTF8.GetBytes("Hello world!");
            const string correlationId = "CorrelationId";
            var messageTypes = new Dictionary<string, Type> { { messageType, typeof(MyMessage) } };

            var message = new Message<MyMessage>(new MyMessage())
            {
                Properties = { CorrelationId = correlationId }
            };
            var serializationStrategy = CreateSerializationStrategy(message, messageTypes, serializedMessageBody, "SomeOtherCorrelationId");

            var serializedMessage = serializationStrategy.SerializeMessage(message);

            AssertMessageSerializedCorrectly(serializedMessage, serializedMessageBody, p => AssertDefaultMessagePropertiesCorrect(p, messageType, correlationId));
        }

        [Test]
        public void When_using_the_versioned_serialization_strategy_messages_are_correctly_deserialized()
        {
            const string messageType = "MyMessageTypeName";
            const string messageContent = "Hello world!";
            var serializedMessageBody = Encoding.UTF8.GetBytes(messageContent);
            const string correlationId = "CorrelationId";
            var messageTypes = new Dictionary<string, Type> { { messageType, typeof(MyMessage) } };

            var message = new Message<MyMessage>(new MyMessage { Text = messageContent })
            {
                Properties =
                {
                    Type = messageType,
                    CorrelationId = correlationId,
                    UserId = "Bob"
                },
            };
            var serializationStrategy = CreateDeserializationStrategy(message.Body, messageTypes, messageType, serializedMessageBody);

            var deserializedMessage = serializationStrategy.DeserializeMessage(message.Properties, serializedMessageBody);

            AssertMessageDeserializedCorrectly((Message<MyMessage>)deserializedMessage, messageContent, typeof(MyMessage), p => AssertDefaultMessagePropertiesCorrect(p, messageType, correlationId));
            Assert.That(deserializedMessage.Properties.UserId, Is.EqualTo(message.Properties.UserId), "Additional message properties not serialised");
        }

        [Test]
        public void When_using_the_versioned_serialization_strategy_messages_are_correctly_round_tripped()
        {
            var typeNameSerializer = new TypeNameSerializer();
            var serializer = new JsonSerializer(typeNameSerializer);

            const string correlationId = "CorrelationId";

            var serializationStrategy = new VersionedMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(correlationId));

            var messageBody = new MyMessage { Text = "Hello world!" };
            var message = new Message<MyMessage>(messageBody);
            var serializedMessage = serializationStrategy.SerializeMessage(message);
            var deserializedMessage = serializationStrategy.DeserializeMessage(serializedMessage.Properties, serializedMessage.Body);

            Assert.That(deserializedMessage.MessageType, Is.EqualTo(message.Body.GetType()));
            Assert.That(((Message<MyMessage>)deserializedMessage).Body.Text, Is.EqualTo(message.Body.Text));
        }

        [Test]
        public void When_using_the_versioned_serialization_strategy_versioned_messages_are_correctly_serialized()
        {
            const string messageType = "MyMessageV2TypeName";
            const string supersededMessageType = "MyMessageTypeName";
            var serializedMessageBody = Encoding.UTF8.GetBytes("Hello world!");
            const string correlationId = "CorrelationId";
            var messageTypes = new Dictionary<string, Type>
                {
                    {messageType, typeof( MyMessageV2 )},
                    {supersededMessageType, typeof( MyMessage )}
                };

            var message = new Message<MyMessageV2>(new MyMessageV2());
            var serializationStrategy = CreateSerializationStrategy(message, messageTypes, serializedMessageBody, correlationId);

            var serializedMessage = serializationStrategy.SerializeMessage(message);

            AssertMessageSerializedCorrectly(serializedMessage, serializedMessageBody, p => AssertVersionedMessagePropertiesCorrect(p, messageType, correlationId, supersededMessageType));
        }

        [Test]
        public void When_serializing_a_versioned_message_with_a_correlationid_it_is_not_overwritten()
        {
            const string messageType = "MyMessageV2TypeName";
            const string supersededMessageType = "MyMessageTypeName";
            var serializedMessageBody = Encoding.UTF8.GetBytes("Hello world!");
            const string correlationId = "CorrelationId";
            var messageTypes = new Dictionary<string, Type>
                {
                    {messageType, typeof( MyMessageV2 )},
                    {supersededMessageType, typeof( MyMessage )}
                };

            var message = new Message<MyMessageV2>(new MyMessageV2())
                {
                    Properties = { CorrelationId = correlationId }
                };
            var serializationStrategy = CreateSerializationStrategy(message, messageTypes, serializedMessageBody, "SomeOtherCorrelationId");

            var serializedMessage = serializationStrategy.SerializeMessage(message);

            AssertMessageSerializedCorrectly(serializedMessage, serializedMessageBody, p => AssertVersionedMessagePropertiesCorrect(p, messageType, correlationId, supersededMessageType));
        }

        [Test]
        public void When_using_the_versioned_serialization_strategy_versioned_messages_are_correctly_deserialized()
        {
            const string messageType = "MyMessageV2TypeName";
            const string supersededMessageType = "MyMessageTypeName";
            const string messageContent = "Hello world!";
            var serializedMessageBody = Encoding.UTF8.GetBytes(messageContent);
            const string correlationId = "CorrelationId";
            var messageTypes = new Dictionary<string, Type>
                {
                    {messageType, typeof( MyMessageV2 )},
                    {supersededMessageType, typeof( MyMessage )}
                };

            var message = new Message<MyMessageV2>(new MyMessageV2 { Text = messageContent })
            {
                Properties =
                {
                    Type = messageType,
                    CorrelationId = correlationId,
                    UserId = "Bob",
                },
            };
            message.Properties.Headers.Add("Alternative-Message-Types", Encoding.UTF8.GetBytes(supersededMessageType));
            var serializationStrategy = CreateDeserializationStrategy(message.Body, messageTypes, messageType, serializedMessageBody);

            var deserializedMessage = serializationStrategy.DeserializeMessage(message.Properties, serializedMessageBody);

            AssertMessageDeserializedCorrectly((Message<MyMessageV2>)deserializedMessage, messageContent, typeof(MyMessageV2), p => AssertVersionedMessagePropertiesCorrect(p, messageType, correlationId, supersededMessageType));
        }

        [Test]
        public void When_using_the_versioned_serialization_strategy_versioned_messages_are_correctly_round_tripped()
        {
            var typeNameSerializer = new TypeNameSerializer();
            var serializer = new JsonSerializer(typeNameSerializer);
            const string correlationId = "CorrelationId";

            var serializationStrategy = new VersionedMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(correlationId));

            var messageBody = new MyMessageV2 { Text = "Hello world!", Number = 5 };
            var message = new Message<MyMessageV2>(messageBody);
            var serializedMessage = serializationStrategy.SerializeMessage(message);

            // RMQ converts the Header values into a byte[] so mimic the translation here
            var alternativeMessageHeader = (string)serializedMessage.Properties.Headers[AlternativeMessageTypesHeaderKey];
            serializedMessage.Properties.Headers[AlternativeMessageTypesHeaderKey] = Encoding.UTF8.GetBytes(alternativeMessageHeader);

            var deserializedMessage = serializationStrategy.DeserializeMessage(serializedMessage.Properties, serializedMessage.Body);

            Assert.That(deserializedMessage.MessageType, Is.EqualTo(message.Body.GetType()));
            Assert.That(((Message<MyMessageV2>)deserializedMessage).Body.Text, Is.EqualTo(message.Body.Text));
            Assert.That(((Message<MyMessageV2>)deserializedMessage).Body.Number, Is.EqualTo(message.Body.Number));
        }

        [Test]
        public void When_deserializing_versioned_message_use_first_available_message_type()
        {
            var typeNameSerializer = new TypeNameSerializer();
            var serializer = new JsonSerializer(typeNameSerializer);
            const string correlationId = "CorrelationId";

            var serializationStrategy = new VersionedMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(correlationId));

            var messageBody = new MyMessageV2 { Text = "Hello world!", Number = 5 };
            var message = new Message<MyMessageV2>(messageBody);
            var serializedMessage = serializationStrategy.SerializeMessage(message);

            // Mess with the properties to mimic a message serialised as MyMessageV3
            var messageType = serializedMessage.Properties.Type;
            serializedMessage.Properties.Type = messageType.Replace("MyMessageV2", "SomeCompletelyRandomType");
            var alternativeMessageHeader = (string)serializedMessage.Properties.Headers[AlternativeMessageTypesHeaderKey];
            alternativeMessageHeader = string.Concat(messageType, ";", alternativeMessageHeader);
            serializedMessage.Properties.Headers[AlternativeMessageTypesHeaderKey] = Encoding.UTF8.GetBytes(alternativeMessageHeader);

            var deserializedMessage = serializationStrategy.DeserializeMessage(serializedMessage.Properties, serializedMessage.Body);

            Assert.That(deserializedMessage.MessageType, Is.EqualTo(typeof(MyMessageV2)));
            Assert.That(((Message<MyMessageV2>)deserializedMessage).Body.Text, Is.EqualTo(message.Body.Text));
            Assert.That(((Message<MyMessageV2>)deserializedMessage).Body.Number, Is.EqualTo(message.Body.Number));
        }

        private void AssertMessageSerializedCorrectly(SerializedMessage message, byte[] expectedBody, Action<MessageProperties> assertMessagePropertiesCorrect)
        {
            Assert.That(message.Body, Is.EqualTo(expectedBody), "Serialized message body does not match expected value");
            assertMessagePropertiesCorrect(message.Properties);
        }

        private void AssertMessageDeserializedCorrectly(IMessage<MyMessage> message, string expectedBodyText, Type expectedMessageType, Action<MessageProperties> assertMessagePropertiesCorrect)
        {
            Assert.That(message.Body.Text, Is.EqualTo(expectedBodyText), "Deserialized message body text does not match expected value");
            Assert.That(message.MessageType, Is.EqualTo(expectedMessageType), "Deserialized message type does not match expected value");

            assertMessagePropertiesCorrect(message.Properties);
        }

        private void AssertDefaultMessagePropertiesCorrect(MessageProperties properties, string expectedType, string expectedCorrelationId)
        {
            Assert.That(properties.Type, Is.EqualTo(expectedType), "Message type does not match expected value");
            Assert.That(properties.CorrelationId, Is.EqualTo(expectedCorrelationId), "Message correlation id does not match expected value");
        }

        private void AssertVersionedMessagePropertiesCorrect(MessageProperties properties, string expectedType, string expectedCorrelationId, string alternativeTypes)
        {
            AssertDefaultMessagePropertiesCorrect(properties, expectedType, expectedCorrelationId);
            Assert.That(properties.Headers[AlternativeMessageTypesHeaderKey], Is.EqualTo(alternativeTypes), "Alternative message types do not match expected value");
        }

        private VersionedMessageSerializationStrategy CreateSerializationStrategy<T>(IMessage<T> message, IEnumerable<KeyValuePair<string, Type>> messageTypes, byte[] messageBody, string correlationId) where T : class
        {
            var typeNameSerializer = MockRepository.GenerateStub<ITypeNameSerializer>();
            foreach (var messageType in messageTypes)
            {
                var localMessageType = messageType;
                typeNameSerializer.Stub(s => s.Serialize(localMessageType.Value)).Return(localMessageType.Key);
            }

            var serializer = MockRepository.GenerateStub<ISerializer>();
            serializer.Stub(s => s.MessageToBytes(message.GetBody())).Return(messageBody);

            return new VersionedMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(correlationId));
        }

        private VersionedMessageSerializationStrategy CreateDeserializationStrategy<T>(T message, IEnumerable<KeyValuePair<string, Type>> messageTypes, string expectedMessageType, byte[] messageBody) where T : class
        {
            var typeNameSerializer = MockRepository.GenerateStub<ITypeNameSerializer>();
            foreach (var messageType in messageTypes)
            {
                var localMessageType = messageType;
                typeNameSerializer.Stub(s => s.DeSerialize(localMessageType.Key)).Return(localMessageType.Value);
            }


            var serializer = MockRepository.GenerateStub<ISerializer>();
            serializer.Stub(s => s.BytesToMessage(expectedMessageType, messageBody)).Return(message);

            return new VersionedMessageSerializationStrategy(typeNameSerializer, serializer, new StaticCorrelationIdGenerationStrategy(String.Empty));
        }
    }
}