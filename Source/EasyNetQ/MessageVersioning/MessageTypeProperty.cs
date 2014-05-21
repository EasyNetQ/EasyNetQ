using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyNetQ.MessageVersioning
{
	public class MessageTypeProperty
	{
		private const string AlternativeMessageTypesHeaderKey = "Alternative-Message-Types";
		private const string AlternativeMessageTypeSeparator = ";";

		private readonly ITypeNameSerializer _typeNameSerializer;
		private readonly string _messageType;
		private readonly List<string> _alternativeTypes;

		private MessageTypeProperty( ITypeNameSerializer typeNameSerializer, Type messageType )
		{
			_typeNameSerializer = typeNameSerializer;
			var messageVersions = new MessageVersionStack( messageType );
			// MessageVersionStack has most recent version at the bottom of the stack (hence the reverse)
			// and includes the actual message type (hence the first / removeat)
			_alternativeTypes = messageVersions
				.Select( typeNameSerializer.Serialize )
				.Reverse()
				.ToList();
			_messageType = _alternativeTypes.First();
			_alternativeTypes.RemoveAt( 0 );
		}

		private MessageTypeProperty( ITypeNameSerializer typeNameSerializer, string messageType, string alternativeTypesHeader )
		{
			_typeNameSerializer = typeNameSerializer;
			_messageType = messageType;
			_alternativeTypes = new List<string>();

			if( !string.IsNullOrWhiteSpace( alternativeTypesHeader ) )
				_alternativeTypes = alternativeTypesHeader
					.Split( new[] {AlternativeMessageTypeSeparator}, StringSplitOptions.RemoveEmptyEntries )
					.ToList();
		}

		public void AppendTo( MessageProperties messageProperties )
		{
			messageProperties.Type = _messageType;

			if( _alternativeTypes.Any() )
				messageProperties.Headers[ AlternativeMessageTypesHeaderKey ] = string.Join( AlternativeMessageTypeSeparator, _alternativeTypes );
		}

		public MessageType GetMessageType()
		{
			Type messageType;
			if( TryGetType( _messageType, out messageType ) )
				return new MessageType {Type = messageType, TypeString = _messageType};

			foreach( var alternativeType in _alternativeTypes )
			{
				if( TryGetType( alternativeType, out messageType ) )
					return new MessageType {Type = messageType, TypeString = alternativeType};
			}

			throw new EasyNetQException("Cannot find declared message type {0} or any of the specified alternative types {1}", _messageType, string.Join( AlternativeMessageTypeSeparator, _alternativeTypes));
		}

		public static MessageTypeProperty CreateForMessageType(Type messageType, ITypeNameSerializer typeNameSerializer)
		{
			return new MessageTypeProperty( typeNameSerializer, messageType );
		}

		public static MessageTypeProperty ExtractFromProperties( MessageProperties messageProperties, ITypeNameSerializer typeNameSerializer )
		{
			var messageType = messageProperties.Type;
			if( !messageProperties.HeadersPresent || !messageProperties.Headers.ContainsKey( AlternativeMessageTypesHeaderKey ) )
				return new MessageTypeProperty( typeNameSerializer, messageType, null );

			var rawHeader = messageProperties.Headers[ AlternativeMessageTypesHeaderKey ] as byte[];
			if( rawHeader == null )
				throw new EasyNetQException( "{0} header was present but contained no data or was not encoded as a byte[].", AlternativeMessageTypesHeaderKey );

			var alternativeTypesHeader = Encoding.UTF8.GetString( rawHeader );
			return new MessageTypeProperty( typeNameSerializer, messageType, alternativeTypesHeader );
		}

		private bool TryGetType(string typeString, out Type messageType)
		{
			try
			{
				messageType = _typeNameSerializer.DeSerialize(typeString);
				return true;
			}
			catch
			{
				messageType = null;
				return false;
			}
		}
	}
}