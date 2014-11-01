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

        public SerializedMessage SerializeMessage<T>(IMessage<T> message) where T : class
        {
            var typeName = typeNameSerializer.Serialize(message.Body.GetType());
            var messageBody = serializer.MessageToBytes(message.Body);
            var messageProperties = message.Properties;

            messageProperties.Type = typeName;
            if (string.IsNullOrEmpty(messageProperties.CorrelationId))
                messageProperties.CorrelationId = correlationIdGenerator.GetCorrelationId();

            return new SerializedMessage(messageProperties, messageBody);
        }

        public DeserializedMessage DeserializeMessage(MessageProperties properties, byte[] body)
        {
            var messageType = typeNameSerializer.DeSerialize(properties.Type);
            var messageBody = serializer.BytesToMessage(properties.Type, body);
            var message = Message.CreateInstance(messageType, messageBody);
            message.SetProperties(properties);
            return new DeserializedMessage(messageType, message);
        }
    }
}