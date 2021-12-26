// ReSharper disable InconsistentNaming
using System.Collections.Generic;
using EasyNetQ.Serialization.NewtonsoftJson;
using FluentAssertions;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Serialization.Tests;

public class SerializerTests
{
    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_and_deserialize_a_default_message(ISerializer serializer)
    {
        using var serializedMessage = serializer.MessageToBytes(typeof(Message), default(Message));
        var deserializedMessage = (Message)serializer.BytesToMessage(typeof(Message), serializedMessage.Memory);
        deserializedMessage.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_and_deserialize_a_message(ISerializer serializer)
    {
        var message = new Message { Text = "Hello World" };

        using var serializedMessage = serializer.MessageToBytes(typeof(Message), message);
        var deserializedMessage = (Message)serializer.BytesToMessage(typeof(Message), serializedMessage.Memory);

        message.Text.Should().Be(deserializedMessage.Text);
    }

    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_basic_properties(ISerializer serializer)
    {
        var originalProperties = new EasyNetQ.Tests.BasicProperties
        {
            AppId = "some app id",
            ClusterId = "cluster id",
            ContentEncoding = "content encoding",
            //ContentType = "content type",
            CorrelationId = "correlation id",
            DeliveryMode = 4,
            Expiration = "1",
            MessageId = "message id",
            Priority = 1,
            ReplyTo = "abc",
            Timestamp = new AmqpTimestamp(123344044),
            Type = "Type",
            UserId = "user id",
            Headers = new Dictionary<string, object>
            {
                { "one", "header one" },
                { "two", "header two" }
            }
        };

        var messageBasicProperties = new MessageProperties();
        messageBasicProperties.CopyFrom(originalProperties);
        using var serializedMessage = serializer.MessageToBytes(typeof(MessageProperties), messageBasicProperties);
        var deserializedMessageBasicProperties = (MessageProperties)serializer.BytesToMessage(
            typeof(MessageProperties), serializedMessage.Memory
        );

        var newProperties = new EasyNetQ.Tests.BasicProperties();
        deserializedMessageBasicProperties.CopyTo(newProperties);

        originalProperties.Should().BeEquivalentTo(newProperties);
    }

    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties(ISerializer serializer)
    {
        using var serializedMessage = serializer.MessageToBytes(typeof(PolyMessage), new PolyMessage { AorB = new B() });
        var result = (PolyMessage)serializer.BytesToMessage(typeof(PolyMessage), serializedMessage.Memory);
        Assert.IsType<B>(result.AorB);
    }

    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties_when_using_TypeNameSerializer(ISerializer serializer)
    {
        using var serializedMessage = serializer.MessageToBytes(typeof(PolyMessage), new PolyMessage { AorB = new B() });
        var result = (PolyMessage)serializer.BytesToMessage(typeof(PolyMessage), serializedMessage.Memory);
        Assert.IsType<B>(result.AorB);
    }

    public static IEnumerable<object[]> GetSerializers()
    {
        yield return new object[] { new NewtonsoftJsonSerializer() };
        yield return new object[] { new JsonSerializer() };
    }


    private class A { }
    private class B : A { }
    private class PolyMessage
    {
        public A AorB { get; set; }
    }
    private class Message
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming
