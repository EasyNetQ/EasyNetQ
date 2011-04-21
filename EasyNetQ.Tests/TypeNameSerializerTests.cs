// ReSharper disable InconsistentNaming
using System;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class TypeNameSerializerTests
    {
        const string expectedTypeName = "System_String:mscorlib";

        [Test]
        public void Should_serialize_a_type_name()
        {
            var typeName = TypeNameSerializer.Serialize(typeof (string));
            typeName.ShouldEqual(expectedTypeName);
        }

        private const string expectedCustomTypeName = "EasyNetQ_TypeNameSerializer:EasyNetQ";

        [Test]
        public void Should_serialize_a_custom_type()
        {
            var typeName = TypeNameSerializer.Serialize(typeof (TypeNameSerializer));
            typeName.ShouldEqual(expectedCustomTypeName);
        }

        public void GetTypeSpike()
        {
            var type = Type.GetType("EasyNetQ.Tests.MyCustomType");
            type.ShouldNotBeNull();
        }
    }
}
// ReSharper restore InconsistentNaming
