// ReSharper disable InconsistentNaming
using System;
using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;
using Xunit;
using System.Reflection;

namespace EasyNetQ.Tests
{
    public class TypeNameSerializerTests
    {
        private readonly string expectedTypeName = "System.String:" + typeof(string).GetTypeInfo().Assembly.GetName().Name;
        private const string expectedCustomTypeName = "EasyNetQ.Tests.SomeRandomClass:EasyNetQ.Tests";

        private ITypeNameSerializer typeNameSerializer;

        public TypeNameSerializerTests()
        {
            typeNameSerializer = new TypeNameSerializer();
        }

        [Fact]
        public void Should_serialize_a_type_name()
        {
            var typeName = typeNameSerializer.Serialize(typeof(string));
            typeName.ShouldEqual(expectedTypeName);
        }

        [Fact]
        public void Should_serialize_a_custom_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(SomeRandomClass));
            typeName.ShouldEqual(expectedCustomTypeName);
        }

        [Fact]
        public void Should_deserialize_a_type_name()
        {
            var type = typeNameSerializer.DeSerialize(expectedTypeName);
            type.ShouldEqual(typeof (string));
        }

        [Fact]
        public void Should_deserialize_a_custom_type()
        {
            var type = typeNameSerializer.DeSerialize(expectedCustomTypeName);
            type.ShouldEqual(typeof(SomeRandomClass));
        }

        [Fact]
        public void Should_throw_exception_when_type_name_is_not_recognised()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                typeNameSerializer.DeSerialize("EasyNetQ.TypeNameSerializer.None:EasyNetQ");
            });
        }

        [Fact]
        public void Should_throw_if_type_name_is_too_long()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                typeNameSerializer.Serialize(
                typeof(
                    MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes
                    ));
            });
        }

        [Fact]
        public void Should_throw_exception_if_type_name_is_null()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                typeNameSerializer.DeSerialize(null);
            });
        }

        public void Spike()
        {
            var type = Type.GetType("EasyNetQ.Tests.SomeRandomClass, EasyNetQ.Tests");
            type.ShouldEqual(typeof (SomeRandomClass));
        }

        public void Spike2()
        {
            var name = typeof (SomeRandomClass).AssemblyQualifiedName;
            Console.Out.WriteLine(name);
        }
    }

    public class SomeRandomClass
    {
        
    }
}
// ReSharper restore InconsistentNaming
