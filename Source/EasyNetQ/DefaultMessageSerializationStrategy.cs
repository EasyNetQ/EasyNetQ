using System;

namespace EasyNetQ
{
	public class DefaultMessageSerializationStrategy : IMessageSerializationStrategy
	{
		private readonly ITypeNameSerializer _typeNameSerializer;
		private readonly ISerializer _serializer;
		private readonly Func<string> _getCorrelationId;

		public DefaultMessageSerializationStrategy(ITypeNameSerializer typeNameSerializer, ISerializer serializer, Func<string> getCorrelationId)
		{
			_typeNameSerializer = typeNameSerializer;
			_serializer = serializer;
			_getCorrelationId = getCorrelationId;
		}

		public SerializedMessage SerializeMessage<T>(IMessage<T> message) where T : class
		{
			var typeName = _typeNameSerializer.Serialize(message.Body.GetType());
			var messageBody = _serializer.MessageToBytes(message.Body);
			var messageProperties = message.Properties;

			messageProperties.Type = typeName;
			if (string.IsNullOrEmpty(messageProperties.CorrelationId))
				messageProperties.CorrelationId = _getCorrelationId();

			return new SerializedMessage(messageProperties, messageBody);
		}

		public DeserializedMessage DeserializeMessage(MessageProperties properties, byte[] body)
		{
			var messageType = _typeNameSerializer.DeSerialize(properties.Type);
			var messageBody = _serializer.BytesToMessage(properties.Type, body);
			var message = Message.CreateInstance(messageType, messageBody);
			message.SetProperties(properties);
			return new DeserializedMessage(messageType, message);
		}
	}
}