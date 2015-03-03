// ReSharper disable InconsistentNaming
using System;
using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class TypeNameSerializerTests
    {
        const string expectedTypeName = "System.String:mscorlib";
        private const string expectedCustomTypeName = "EasyNetQ.Tests.SomeRandomClass:EasyNetQ.Tests";

        private ITypeNameSerializer typeNameSerializer;

        [SetUp]
        public void SetUp()
        {
            typeNameSerializer = new TypeNameSerializer();
        }

        [Test]
        public void Should_serialize_a_type_name()
        {
            var typeName = typeNameSerializer.Serialize(typeof(string));
            typeName.ShouldEqual(expectedTypeName);
        }

        [Test]
        public void Should_serialize_a_custom_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(SomeRandomClass));
            typeName.ShouldEqual(expectedCustomTypeName);
        }

        [Test]
        public void Should_deserialize_a_type_name()
        {
            var type = typeNameSerializer.DeSerialize(expectedTypeName);
            type.ShouldEqual(typeof (string));
        }

        [Test]
        public void Should_deserialize_a_custom_type()
        {
            var type = typeNameSerializer.DeSerialize(expectedCustomTypeName);
            type.ShouldEqual(typeof(SomeRandomClass));
        }

        [Test]
        [ExpectedException(typeof(EasyNetQException))]
        public void Should_throw_exception_when_type_name_is_not_recognised()
        {
            typeNameSerializer.DeSerialize("EasyNetQ.TypeNameSerializer.None:EasyNetQ");
        }

        [Test]
        [ExpectedException(typeof(EasyNetQException))]
        public void Should_throw_if_type_name_is_too_long()
        {
            typeNameSerializer.Serialize(
                typeof (
                    MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes
                    ));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Should_throw_exception_if_type_name_is_null()
        {
            typeNameSerializer.DeSerialize(null);
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
