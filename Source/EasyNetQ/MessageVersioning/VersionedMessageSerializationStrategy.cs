
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

        public SerializedMessage SerializeMessage(IMessage message)
        {
            var messageBody = serializer.MessageToBytes(message.GetBody());
            var messageTypeProperties = MessageTypeProperty.CreateForMessageType(message.MessageType, typeNameSerializer);
            var messageProperties = message.Properties;
            messageTypeProperties.AppendTo(messageProperties);
            if (string.IsNullOrEmpty(messageProperties.CorrelationId))
            {
                messageProperties.CorrelationId = correlationIdGenerator.GetCorrelationId();
            }
            return new SerializedMessage(messageProperties, messageBody);
        }


        public IMessage DeserializeMessage(MessageProperties properties, byte[] body)
        {
            var messageTypeProperty = MessageTypeProperty.ExtractFromProperties(properties, typeNameSerializer);
            var messageType = messageTypeProperty.GetMessageType();
            var messageBody = serializer.BytesToMessage(messageType.TypeString, body);
            messageTypeProperty.AppendTo(properties);
            return MessageFactory.CreateInstance(messageType.Type, messageBody, properties);
        }
    }
}