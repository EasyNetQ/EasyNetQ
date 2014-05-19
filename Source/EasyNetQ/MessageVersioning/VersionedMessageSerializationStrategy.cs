using System;

namespace EasyNetQ.MessageVersioning
{
	public class VersionedMessageSerializationStrategy : IMessageSerializationStrategy
	{
		private readonly ITypeNameSerializer _typeNameSerializer;
		private readonly ISerializer _serializer;
		private readonly Func<string> _getCorrelationId;

		public VersionedMessageSerializationStrategy(ITypeNameSerializer typeNameSerializer, ISerializer serializer, Func<string> getCorrelationId)
		{
			_typeNameSerializer = typeNameSerializer;
			_serializer = serializer;
			_getCorrelationId = getCorrelationId;
		}

		public SerializedMessage SerializeMessage<T>(IMessage<T> message) where T : class
		{
			var messageBody = _serializer.MessageToBytes(message.Body);

			var messageTypeProperties = MessageTypeProperties.CreateForMessageType( message.Body.GetType(), _typeNameSerializer );
			var messageProperties = message.Properties;
			messageTypeProperties.AppendTo( messageProperties );
			if (string.IsNullOrEmpty(messageProperties.CorrelationId))
				messageProperties.CorrelationId = _getCorrelationId();

			return new SerializedMessage(messageProperties, messageBody);
		}

		public DeserializedMessage DeserializeMessage(MessageProperties properties, byte[] body)
		{
			var messageTypeProperties = MessageTypeProperties.ExtractFromProperties( properties, _typeNameSerializer );
			var messageType = messageTypeProperties.GetMessageType();

			var messageBody = _serializer.BytesToMessage(properties.Type, body);
			var message = Message.CreateInstance(messageType, messageBody);
			message.SetProperties(properties);

			return new DeserializedMessage(messageType, message);
		}	
	}
}