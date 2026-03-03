// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;
using EasyNetQ.Serialization.NewtonsoftJson;
using EasyNetQ.Serialization.SystemTextJson;
using RabbitMQ.Client;

namespace EasyNetQ.Serialization.Tests;

#pragma warning disable xUnit1026 // Theory methods should use all of their parameters

public class SerializerTests
{
    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_and_deserialize_a_message(string name, ISerializer serializer)
    {
        var message = new Message { Text = "Hello World" };

        using var serializedMessage = serializer.MessageToBytes(typeof(Message), message);
        var deserializedMessage = (Message)serializer.BytesToMessage(typeof(Message), serializedMessage.Memory);

        message.Text.Should().Be(deserializedMessage.Text);
    }

    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_message_properties_simple(string name, ISerializer serializer)
    {
        var originalProperties = new EasyNetQ.Tests.BasicProperties
        {
            AppId = "some app id",
            ClusterId = "cluster id",
            ContentEncoding = "content encoding",
            ContentType = "content type",
            CorrelationId = "correlation id",
            DeliveryMode = DeliveryModes.Persistent,
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

        var messageBasicProperties = new MessageProperties(originalProperties);
        using var serializedMessage = serializer.MessageToBytes(typeof(MessageProperties), messageBasicProperties);
        var deserializedMessageBasicProperties = (MessageProperties)serializer.BytesToMessage(
            typeof(MessageProperties), serializedMessage.Memory
        );

        var newProperties = new BasicProperties();
        deserializedMessageBasicProperties.CopyTo(newProperties);

        originalProperties.Should().BeEquivalentTo(newProperties);
    }


    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_message_properties_extended(string name, ISerializer serializer)
    {
        if (name != "SystemTextJsonV2") return;

        var originalProperties = new BasicProperties
        {
            AppId = "some app id",
            ClusterId = "cluster id",
            ContentEncoding = "content encoding",
            ContentType = "content type",
            CorrelationId = "correlation id",
            DeliveryMode = DeliveryModes.Persistent,
            Expiration = "1",
            MessageId = "message id",
            Priority = 1,
            ReplyTo = "abc",
            Timestamp = new AmqpTimestamp(123344044),
            Type = "Type",
            UserId = "user id",
            Headers = new Dictionary<string, object>
            {
                { "Bool", false },
                { "Byte", (byte)1 },
                { "Sbyte", (sbyte)2 },
                { "Int16", (short)3 },
                { "Int32", 4 },
                { "Uint32", 5U },
                { "Int64", 6L },
                { "Single", 1F },
                { "Double", 8D },
                { "Decimal", 9M },
                { "AmqpTimestamp", new AmqpTimestamp(10) },
                { "String", "11" },
                { "Bytes", new byte[] { 12 } },
                { "List", new[] { "13" } },
                { "Dictionary", new Dictionary<string, object> { { "14", 15 } } },
                { "BinaryTable", new BinaryTableValue([16])}
            }
        };

        var messageBasicProperties = new MessageProperties(originalProperties);
        using var serializedMessage = serializer.MessageToBytes(typeof(MessageProperties), messageBasicProperties);

        var deserializedMessageBasicProperties = (MessageProperties)serializer.BytesToMessage(
            typeof(MessageProperties), serializedMessage.Memory
        );

        var newProperties = new BasicProperties();
        deserializedMessageBasicProperties.CopyTo(newProperties);

        originalProperties.Should().BeEquivalentTo(newProperties);
    }


    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_be_able_to_serialize_and_deserialize_polymorphic_properties(string name, ISerializer serializer)
    {
        using var serializedMessage = serializer.MessageToBytes(typeof(PolyMessage), new PolyMessage { AorB = new B() });
        var result = (PolyMessage)serializer.BytesToMessage(typeof(PolyMessage), serializedMessage.Memory);
        Assert.IsType<B>(result.AorB);
    }

    public static IEnumerable<object[]> GetSerializers()
    {
        yield return ["Newtonsoft", new NewtonsoftJsonSerializer()];
        yield return ["SystemTextJson", new SystemTextJsonSerializer()];
        yield return ["SystemTextJsonV2", new SystemTextJsonSerializerV2()];
    }
    [JsonDerivedType(typeof(A), typeDiscriminator: "a")]
    [JsonDerivedType(typeof(B), typeDiscriminator: "b")]
    private class A
    {
    }

    private sealed class B : A
    {
    }

    private sealed class PolyMessage
    {
        public A AorB { get; set; }
    }

    private sealed class Message
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming
