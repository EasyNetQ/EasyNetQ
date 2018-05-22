// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;
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

#if NET_CORE
        [Fact]
        public void Should_serialize_hashset_of_string_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(HashSet<string>));
            typeName.ShouldEqual("System.Collections.Generic.HashSet`1[[System.String, System.Private.CoreLib]], System.Collections");
        }
#else
        [Fact]
        public void Should_serialize_hashset_of_string_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(HashSet<string>));
            typeName.ShouldEqual("System.Collections.Generic.HashSet`1[[System.String, mscorlib]], System.Core");
        }
#endif

#if NET_CORE
        [Fact]
        public void Should_serialize_string_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(string));
            typeName.ShouldEqual("System.String, System.Private.CoreLib");
        }
#else
        [Fact]
        public void Should_serialize_string_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(string));
            typeName.ShouldEqual("System.String, mscorlib");
        }
#endif

        [Fact]
        public void Should_serialize_some_random_class_type()
        {
            var typeName = typeNameSerializer.Serialize(typeof(SomeRandomClass));
            typeName.ShouldEqual("EasyNetQ.Tests.SomeRandomClass, EasyNetQ.Tests");
        }

        [Fact]
        public void Should_deserialize_string_type_name()
        {
            var type = typeNameSerializer.DeSerialize("System.String, mscorlib");
            type.ShouldEqual(typeof (string));
        }

#if NET_CORE
        [Fact]
        public void Should_deserialize_hashset_of_string_type()
        {
            var type = typeNameSerializer.DeSerialize("System.Collections.Generic.HashSet`1[[System.String, System.Private.CoreLib]], System.Collections");
            type.ShouldEqual(typeof(HashSet<string>));
        }
#else
        [Fact]
        public void Should_deserialize_hashset_of_string_type()
        {
            var type = typeNameSerializer.DeSerialize("System.Collections.Generic.HashSet`1[[System.String, mscorlib]], System.Core");
            type.ShouldEqual(typeof(HashSet<string>));
        }
#endif

        [Fact]
        public void Should_deserialize_some_random_class_type_name()
        {
            var type = typeNameSerializer.DeSerialize("EasyNetQ.Tests.SomeRandomClass, EasyNetQ.Tests");
            type.ShouldEqual(typeof(SomeRandomClass));
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
