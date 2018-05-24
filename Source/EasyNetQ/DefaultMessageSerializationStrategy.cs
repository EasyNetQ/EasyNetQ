namespace EasyNetQ
{
    public class DefaultMessageSerializationStrategy : IMessageSerializationStrategy
    {
        private readonly ITypeNameSerializer typeNameSerializer;
        private readonly ISerializer serializer;
        private readonly ICorrelationIdGenerationStrategy correlationIdGenerator;

        public DefaultMessageSerializationStrategy(ITypeNameSerializer typeNameSerializer, ISerializer serializer, ICorrelationIdGenerationStrategy correlationIdGenerator)
        {
            this.typeNameSerializer = typeNameSerializer;
            this.serializer = serializer;
            this.correlationIdGenerator = correlationIdGenerator;
        }

        public SerializedMessage SerializeMessage(IMessage message)
        {
            var typeName = typeNameSerializer.Serialize(message.MessageType);
            var messageBody = serializer.MessageToBytes(message.GetBody());
            var messageProperties = message.Properties;

            messageProperties.Type = typeName;
            if (string.IsNullOrEmpty(messageProperties.CorrelationId))
            {
                messageProperties.CorrelationId = correlationIdGenerator.GetCorrelationId();
            }
            return new SerializedMessage(messageProperties, messageBody);
        }

        public IMessage DeserializeMessage(MessageProperties properties, byte[] body)
        {
            var messageType = typeNameSerializer.DeSerialize(properties.Type);
            var messageBody = serializer.BytesToMessage(messageType, body);
            return MessageFactory.CreateInstance(messageType, messageBody, properties);
        }
    }
}