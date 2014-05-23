using System;

namespace EasyNetQ
{
    public class DefaultMessageSerializationStrategy : IMessageSerializationStrategy
    {
        private readonly ITypeNameSerializer typeNameSerializer;
        private readonly ISerializer serializer;
        private readonly Func<string> getCorrelationId;

        public DefaultMessageSerializationStrategy(ITypeNameSerializer typeNameSerializer, ISerializer serializer, Func<string> getCorrelationId)
        {
            this.typeNameSerializer = typeNameSerializer;
            this.serializer = serializer;
            this.getCorrelationId = getCorrelationId;
        }

        public SerializedMessage SerializeMessage<T>(IMessage<T> message) where T : class
        {
            var typeName = typeNameSerializer.Serialize(message.Body.GetType());
            var messageBody = serializer.MessageToBytes(message.Body);
            var messageProperties = message.Properties;

            messageProperties.Type = typeName;
            if (string.IsNullOrEmpty(messageProperties.CorrelationId))
                messageProperties.CorrelationId = getCorrelationId();

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