// ReSharper disable InconsistentNaming

using System.Text;
using EasyNetQ.MessageVersioning;
using NUnit.Framework;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    [TestFixture]
    public class MessageTypePropertyTests
    {
        private const string AlternativeMessageTypesHeaderKey = "Alternative-Message-Types";

        // All types missing - GetType == exception
        [Test]
        public void GetMessageType_returns_message_type_for_an_unversioned_message()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var property = MessageTypeProperty.CreateForMessageType( typeof( MyMessage ), typeNameSerialiser );

            var messageType = property.GetMessageType();
            Assert.That( messageType.Type, Is.EqualTo( typeof( MyMessage ) ) );
        }

        [Test]
        public void AppendTo_sets_message_type_and_no_alternatives_for_an_unversioned_message()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var property = MessageTypeProperty.CreateForMessageType( typeof( MyMessage ), typeNameSerialiser );
            var properties = new MessageProperties();

            property.AppendTo( properties );

            Assert.That( properties.Type, Is.EqualTo( typeNameSerialiser.Serialize( typeof( MyMessage ) ) ) );
            Assert.That( properties.Headers.ContainsKey( AlternativeMessageTypesHeaderKey ), Is.False );
        }

        [Test]
        public void GetMessageType_returns_message_type_for_a_versioned_message()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var property = MessageTypeProperty.CreateForMessageType( typeof( MyMessageV2 ), typeNameSerialiser );

            var messageType = property.GetMessageType();
            Assert.That( messageType.Type, Is.EqualTo( typeof( MyMessageV2 ) ) );
        }

        [Test]
        public void AppendTo_sets_message_type_and_alternatives_for_a_versioned_message()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var property = MessageTypeProperty.CreateForMessageType( typeof( MyMessageV2 ), typeNameSerialiser );
            var properties = new MessageProperties();

            property.AppendTo( properties );

            Assert.That( properties.Type, Is.EqualTo( typeNameSerialiser.Serialize( typeof( MyMessageV2 ) ) ) );
            Assert.That( properties.Headers[ AlternativeMessageTypesHeaderKey ], Is.EqualTo( typeNameSerialiser.Serialize( typeof( MyMessage ) ) ) );
        }

        [Test]
        public void MessageTypeProperty_is_created_correctly_from_message_properties_for_unversioned_message()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var properties = new MessageProperties {Type = typeNameSerialiser.Serialize( typeof( MyMessage ) )};

            var property = MessageTypeProperty.ExtractFromProperties( properties, typeNameSerialiser );
            var messageType = property.GetMessageType();

            Assert.That( messageType.Type, Is.EqualTo( typeof( MyMessage ) ) );
        }

        [Test]
        public void MessageTypeProperty_is_created_correctly_from_message_properties_for_versioned_message()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var properties = new MessageProperties {Type = typeNameSerialiser.Serialize( typeof( MyMessageV2 ) )};
            var encodedAlternativeMessageTypes = Encoding.UTF8.GetBytes( typeNameSerialiser.Serialize( typeof( MyMessage ) ) );
            properties.Headers.Add( AlternativeMessageTypesHeaderKey, encodedAlternativeMessageTypes );

            var property = MessageTypeProperty.ExtractFromProperties( properties, typeNameSerialiser );
            var messageType = property.GetMessageType();

            Assert.That( messageType.Type, Is.EqualTo( typeof( MyMessageV2 ) ) );
        }

        [Test]
        public void GetType_returns_first_available_alternative_if_message_type_unavailable()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var v1 = typeNameSerialiser.Serialize( typeof( MyMessage ) );
            var v2 = typeNameSerialiser.Serialize( typeof( MyMessageV2 ) );
            var vUnknown = v2.Replace( "MyMessageV2", "MyUnknownMessage" );
            var alternativeTypes = string.Concat( v2, ";", v1 );
            var encodedAlternativeTypes = Encoding.UTF8.GetBytes( alternativeTypes );

            var properties = new MessageProperties {Type = vUnknown };
            properties.Headers.Add( AlternativeMessageTypesHeaderKey, encodedAlternativeTypes );

            var property = MessageTypeProperty.ExtractFromProperties( properties, typeNameSerialiser );
            var messageType = property.GetMessageType();

            Assert.That( messageType.Type, Is.EqualTo( typeof( MyMessageV2 ) ) );
        }

        [Test]
        public void GetType_returns_first_available_alternative_if_message_type_and_some_alternatives_unavailable()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var v1 = typeNameSerialiser.Serialize( typeof( MyMessage ) );
            var v2 = typeNameSerialiser.Serialize( typeof( MyMessageV2 ) );
            var vUnknown1 = v2.Replace( "MyMessageV2", "MyUnknownMessage" );
            var vUnknown2 = v2.Replace( "MyMessageV2", "MyUnknownMessageV2" );
            var alternativeTypes = string.Concat( vUnknown1, ";", v2, ";", v1 );
            var encodedAlternativeTypes = Encoding.UTF8.GetBytes( alternativeTypes );

            var properties = new MessageProperties {Type = vUnknown2 };
            properties.Headers.Add( AlternativeMessageTypesHeaderKey, encodedAlternativeTypes );

            var property = MessageTypeProperty.ExtractFromProperties( properties, typeNameSerialiser );
            var messageType = property.GetMessageType();

            Assert.That( messageType.Type, Is.EqualTo( typeof( MyMessageV2 ) ) );
        }

        [Test]
        public void GetType_throws_exception_if_all_types_unavailable()
        {
            var typeNameSerialiser = new TypeNameSerializer();
            var v2 = typeNameSerialiser.Serialize( typeof( MyMessageV2 ) );
            var vUnknown1 = v2.Replace( "MyMessageV2", "MyUnknownMessage" );
            var vUnknown2 = v2.Replace( "MyMessageV2", "MyUnknownMessageV2" );
            var vUnknown3 = v2.Replace( "MyMessageV2", "MyUnknownMessageV3" );
            var alternativeTypes = string.Concat( vUnknown2, ";", vUnknown1 );
            var encodedAlternativeTypes = Encoding.UTF8.GetBytes( alternativeTypes );

            var properties = new MessageProperties {Type = vUnknown3 };
            properties.Headers.Add( AlternativeMessageTypesHeaderKey, encodedAlternativeTypes );

            var property = MessageTypeProperty.ExtractFromProperties( properties, typeNameSerialiser );
            Assert.Throws<EasyNetQException>( () => property.GetMessageType() );
        }
    }
}