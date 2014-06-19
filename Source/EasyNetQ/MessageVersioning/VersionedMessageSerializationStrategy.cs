using System;

namespace EasyNetQ.MessageVersioning
{
    public class VersionedMessageSerializationStrategy : IMessageSerializationStrategy
    {
        private readonly ITypeNameSerializer typeNameSerializer;
        private readonly ISerializer serializer;
        private readonly ICorrelationIdGenerationStrategy correlationIdGenerator;

        public VersionedMessageSerializationStrategy(ITypeNameSerializer typeNameSerializer, ISerializer serializer, ICorrelationIdGenerationStrategy correlationIdGenerator)
        {
            this.typeNameSerializer = typeNameSerializer;
            this.serializer = serializer;
            this.correlationIdGenerator = correlationIdGenerator;
        }

        public SerializedMessage SerializeMessage<T>(IMessage<T> message) where T : class
        {
            var messageBody = serializer.MessageToBytes(message.Body);

            var messageTypeProperties = MessageTypeProperty.CreateForMessageType( message.Body.GetType(), typeNameSerializer );
            var messageProperties = message.Properties;
            messageTypeProperties.AppendTo( messageProperties );
            if (string.IsNullOrEmpty(messageProperties.CorrelationId))
                messageProperties.CorrelationId = correlationIdGenerator.GetCorrelationId();

            return new SerializedMessage(messageProperties, messageBody);
        }

        public DeserializedMessage DeserializeMessage(MessageProperties properties, byte[] body)
        {
            var messageTypeProperty = MessageTypeProperty.ExtractFromProperties( properties, typeNameSerializer );
            var messageType = messageTypeProperty.GetMessageType();

            var messageBody = serializer.BytesToMessage( messageType.TypeString, body );
            var message = Message.CreateInstance( messageType.Type, messageBody );
            // Replace the raw message type property data with our deserialised version
            messageTypeProperty.AppendTo( properties );
            message.SetProperties( properties );

            return new DeserializedMessage( messageType.Type, message );
        }	
    }
}