using EasyNetQ.Pipeline;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests;

/// <summary>
///     Typed publishes run through the publish pipeline with the message and its descriptor on the context;
///     SerializeStep serializes inside the pipeline and stamps the wire type name.
/// </summary>
public class TypedPublishPipelineTests
{
    private const string correlationId = "abc123";

    [DeliveryMode(isPersistent: false)]
    private sealed class TransientMessage
    {
        public string Text { get; set; } = "";
    }

    [Fact]
    public async Task Should_expose_typed_message_to_pipeline_steps_and_serialize_in_pipeline()
    {
        object? messageSeen = null;
        MessageTypeDescriptor? descriptorSeen = null;
        var bodyLengthSeen = -1;

        var pipelineBuilder = new PipelineBuilder<PublishContext>().Use("probe", (context, next) =>
        {
            messageSeen = context.Message;
            descriptorSeen = context.MessageType;
            bodyLengthSeen = context.Body.Length;
            return next(context);
        });

        await using var mockBuilder = new MockBuilder(x => x.AddSingleton(pipelineBuilder).AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId)));

        var message = new MyMessage { Text = "Hiya!" };
        await mockBuilder.Bus.Advanced.PublishAsync(
            "the.exchange", "the.routing.key", null, null, MessageProperties.Empty, message, CancellationToken.None
        );

        var expectedBody = "{\"Text\":\"Hiya!\"}"u8.ToArray();
        messageSeen.Should().BeSameAs(message);
        descriptorSeen!.Type.Should().Be<MyMessage>();
        bodyLengthSeen.Should().Be(0, "the probe runs before SerializeStep");

        mockBuilder.PublishedOn(mockBuilder.PublishChannel).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage(
                "the.exchange",
                "the.routing.key",
                false,
                new RabbitMQ.Client.BasicProperties { CorrelationId = correlationId, Type = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests" },
                expectedBody
            )
        );
    }

    [Fact]
    public async Task Should_stamp_delivery_mode_declared_by_the_message_type()
    {
        await using var mockBuilder = new MockBuilder(x => x.AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId)));

        await mockBuilder.Bus.Advanced.PublishAsync(
            "the.exchange", "rk", null, null, MessageProperties.Empty, new TransientMessage { Text = "t" }, CancellationToken.None
        );

        mockBuilder.PublishedOn(mockBuilder.PublishChannel).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage(
                "the.exchange",
                "rk",
                false,
                new RabbitMQ.Client.BasicProperties
                {
                    CorrelationId = correlationId,
                    Type = "EasyNetQ.Tests.TypedPublishPipelineTests+TransientMessage, EasyNetQ.Tests",
                    DeliveryMode = DeliveryModes.Transient
                },
                "{\"Text\":\"t\"}"u8.ToArray()
            )
        );
    }

    [Fact]
    public async Task Should_not_stamp_delivery_mode_for_undeclared_types_on_direct_publishes()
    {
        await using var mockBuilder = new MockBuilder(x => x.AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId)));

        await mockBuilder.Bus.Advanced.PublishAsync(
            "the.exchange", "rk", null, null, MessageProperties.Empty, new MyMessage { Text = "t" }, CancellationToken.None
        );

        var published = mockBuilder.PublishedOn(mockBuilder.PublishChannel).Should().ContainSingle().Which;
        published.Should().BeEquivalentTo(
            new PublishedMessage(
                "the.exchange",
                "rk",
                false,
                new RabbitMQ.Client.BasicProperties { CorrelationId = correlationId, Type = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests" },
                "{\"Text\":\"t\"}"u8.ToArray()
            )
        );
        published.Properties.IsDeliveryModePresent().Should().BeFalse();
    }

    public static TheoryData<string> ByteBufferBodies => ["byte[]", "Memory<byte>", "ArraySegment<byte>", "ReadOnlyMemory<byte>"];

    [Theory]
    [MemberData(nameof(ByteBufferBodies))]
    public async Task Should_publish_a_byte_buffer_body_as_is(string bufferType)
    {
        await using var mockBuilder = new MockBuilder();
        var bytes = "{\"raw\":true}"u8.ToArray();
        var advanced = mockBuilder.Bus.Advanced;
        var properties = new MessageProperties { Type = "foreign.type.v1" };

        var publish = bufferType switch
        {
            // overload resolution: the exact byte[] overload, not PublishAsync<byte[]>
            "byte[]" => advanced.PublishAsync("x", "rk", null, null, properties, bytes, CancellationToken.None),
            // the typed overload with a buffer type is the raw body too
            "Memory<byte>" => advanced.PublishAsync<Memory<byte>>("x", "rk", null, null, properties, bytes.AsMemory(), CancellationToken.None),
            "ArraySegment<byte>" => advanced.PublishAsync<ArraySegment<byte>>("x", "rk", null, null, properties, new ArraySegment<byte>(bytes), CancellationToken.None),
            _ => advanced.PublishAsync<ReadOnlyMemory<byte>>("x", "rk", null, null, properties, bytes, CancellationToken.None),
        };
        await publish;

        mockBuilder.PublishedOn(mockBuilder.PublishChannel).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage("x", "rk", false, new RabbitMQ.Client.BasicProperties { Type = "foreign.type.v1" }, bytes)
        );
    }
}
