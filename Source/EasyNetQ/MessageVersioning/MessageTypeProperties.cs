using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyNetQ.MessageVersioning
{
	public class MessageTypeProperties
	{
		private const string AlternativeMessageTypesHeaderKey = "Alternative-Message-Types";
		private const string AlternativeMessageTypeSeparator = ";";

		private readonly ITypeNameSerializer _typeNameSerializer;
		private readonly string _messageType;
		private readonly List<string> _alternativeTypes;

		private MessageTypeProperties( ITypeNameSerializer typeNameSerializer, Type messageType )
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

		private MessageTypeProperties( ITypeNameSerializer typeNameSerializer, string messageType, string alternativeTypesHeader )
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
				messageProperties.Headers.Add( AlternativeMessageTypesHeaderKey,
				                               string.Join( AlternativeMessageTypeSeparator, _alternativeTypes ) );
		}

		public Type GetMessageType()
		{
			Type messageType;
			if( TryGetType( _messageType, out messageType ) )
				return messageType;

			var messageTypeFound = _alternativeTypes.Select( t => TryGetType( t, out messageType ) ).FirstOrDefault();
			if( messageTypeFound )
				return messageType;

			throw new EasyNetQException("Cannot find declared message type {0} or any of the specified alternative types {1}", _messageType, string.Join( AlternativeMessageTypeSeparator, _alternativeTypes));
		}

		public static MessageTypeProperties CreateForMessageType(Type messageType, ITypeNameSerializer typeNameSerializer)
		{
			return new MessageTypeProperties( typeNameSerializer, messageType );
		}

		public static MessageTypeProperties ExtractFromProperties( MessageProperties messageProperties, ITypeNameSerializer typeNameSerializer )
		{
			var messageType = messageProperties.Type;
			if( !messageProperties.HeadersPresent || !messageProperties.Headers.ContainsKey( AlternativeMessageTypesHeaderKey ) )
				return new MessageTypeProperties( typeNameSerializer, messageType, null );

			var rawHeader = messageProperties.Headers[ AlternativeMessageTypesHeaderKey ] as byte[];
			if( rawHeader == null )
				throw new EasyNetQException( "{0} header was present but contained no data or was not encoded as a byte[].", AlternativeMessageTypesHeaderKey );

			var alternativeTypesHeader = Encoding.UTF8.GetString( rawHeader );
			return new MessageTypeProperties( typeNameSerializer, messageType, alternativeTypesHeader );
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