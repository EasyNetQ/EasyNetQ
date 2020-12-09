// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests
{
    public class DefaultTypeNameSerializerTests
    {
        private readonly ITypeNameSerializer typeNameSerializer;

        public DefaultTypeNameSerializerTests()
        {
            typeNameSerializer = new DefaultTypeNameSerializer();
        }

        [Fact]
        public void Should_serialize_hashset_of_string_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(HashSet<string>));
            typeName.Should().Be("System.Collections.Generic.HashSet`1[[System.String, System.Private.CoreLib]], System.Collections");
        }

        [Fact]
        public void Should_serialize_string_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(string));
            typeName.Should().Be("System.String, System.Private.CoreLib");
        }

        [Fact]
        public void Should_serialize_some_random_class_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(SomeRandomClass));
            typeName.Should().Be("EasyNetQ.Tests.SomeRandomClass, EasyNetQ.Tests");
        }

        [Fact]
        public void Should_deserialize_string_type_name()
        {
            var type = typeNameSerializer.DeSerialize("System.String, mscorlib");
            type.Should().Be(typeof(string));
        }

        [Fact]
        public void Should_deserialize_hashset_of_string_type()
        {
            var type = typeNameSerializer.DeSerialize("System.Collections.Generic.HashSet`1[[System.String, System.Private.CoreLib]], System.Collections");
            type.Should().Be(typeof(HashSet<string>));
        }

        [Fact]
        public void Should_deserialize_some_random_class_type_name()
        {
            var type = typeNameSerializer.DeSerialize("EasyNetQ.Tests.SomeRandomClass, EasyNetQ.Tests");
            type.Should().Be(typeof(SomeRandomClass));
        }

        [Fact]
        public void Should_throw_exception_when_type_name_is_not_recognised()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                typeNameSerializer.DeSerialize("EasyNetQ.TypeNameSerializer.None, EasyNetQ");
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
    }
}
// ReSharper restore InconsistentNaming
