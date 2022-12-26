// ReSharper disable InconsistentNaming

using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;

namespace EasyNetQ.Tests;

public class LegacyTypeNameSerializerTests
{
    private readonly ITypeNameSerializer typeNameSerializer;

    public LegacyTypeNameSerializerTests()
    {
        typeNameSerializer = new LegacyTypeNameSerializer();
    }

    [Fact]
    public void Should_serialize_hashset_of_string_type()
    {
        var typeName = typeNameSerializer.Serialize(typeof(HashSet<string>));
        typeName.Should().Be("System.Collections.Generic.HashSet`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]:System.Private.CoreLib");
    }

    [Fact]
    public void Should_serialize_string_type()
    {
        var typeName = typeNameSerializer.Serialize(typeof(string));
        typeName.Should().Be("System.String:System.Private.CoreLib");
    }

    [Fact]
    public void Should_serialize_some_random_class_type()
    {
        var typeName = typeNameSerializer.Serialize(typeof(SomeRandomClass));
        typeName.Should().Be("EasyNetQ.Tests.SomeRandomClass:EasyNetQ.Tests");
    }

    [Fact]
    public void Should_deserialize_net45_string_type_name()
    {
        var type = typeNameSerializer.Deserialize("System.String:mscorlib");
        type.Should().Be(typeof(string));
    }

    [Fact]
    public void Should_deserialize_netcore_string_type_name()
    {
        var type = typeNameSerializer.Deserialize("System.String:System.Private.CoreLib");
        type.Should().Be(typeof(string));
    }

    [Fact]
    public void Should_deserialize_hashset_of_string_type()
    {
        var type = typeNameSerializer.Deserialize("System.Collections.Generic.HashSet`1[[System.String, System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]:System.Collections");
        type.Should().Be(typeof(HashSet<string>));
    }

    [Fact]
    public void Should_deserialize_some_random_class_type_name()
    {
        var type = typeNameSerializer.Deserialize("EasyNetQ.Tests.SomeRandomClass:EasyNetQ.Tests");
        type.Should().Be(typeof(SomeRandomClass));
    }

    [Fact]
    public void Should_throw_exception_when_type_name_is_not_recognised()
    {
        Assert.Throws<EasyNetQException>(() =>
        {
            typeNameSerializer.Deserialize("EasyNetQ.TypeNameSerializer.None:EasyNetQ");
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
}
// ReSharper restore InconsistentNaming
