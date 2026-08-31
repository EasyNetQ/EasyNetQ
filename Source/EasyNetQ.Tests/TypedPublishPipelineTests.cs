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
    [DeliveryMode(isPersistent: false)]
    private sealed class TransientMessage
    {
        public string? Text { get; set; }
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

        await using var mockBuilder = new MockBuilder(x => x.AddSingleton(pipelineBuilder));

        var message = new MyMessage { Text = "Hiya!" };
        await mockBuilder.Bus.Advanced.PublishAsync(
            "the.exchange", "the.routing.key", null, null, MessageProperties.Empty, message, CancellationToken.None
        );

        var expectedBody = "{\"Text\":\"Hiya!\"}"u8.ToArray();
        messageSeen.Should().BeSameAs(message);
        descriptorSeen!.Type.Should().Be<MyMessage>();
        bodyLengthSeen.Should().Be(0, "the probe runs before SerializeStep");

        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            Arg.Is("the.exchange"),
            Arg.Is("the.routing.key"),
            Arg.Is(false),
            Arg.Is<RabbitMQ.Client.BasicProperties>(x => x.Type == "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
            Arg.Is<ReadOnlyMemory<byte>>(x => x.ToArray().SequenceEqual(expectedBody)),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_stamp_delivery_mode_declared_by_the_message_type()
    {
        await using var mockBuilder = new MockBuilder();

        await mockBuilder.Bus.Advanced.PublishAsync(
            "the.exchange", "rk", null, null, MessageProperties.Empty, new TransientMessage { Text = "t" }, CancellationToken.None
        );

        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            Arg.Is("the.exchange"),
            Arg.Is("rk"),
            Arg.Is(false),
            Arg.Is<RabbitMQ.Client.BasicProperties>(x => x.DeliveryMode == DeliveryModes.Transient),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_not_stamp_delivery_mode_for_undeclared_types_on_direct_publishes()
    {
        await using var mockBuilder = new MockBuilder();

        await mockBuilder.Bus.Advanced.PublishAsync(
            "the.exchange", "rk", null, null, MessageProperties.Empty, new MyMessage { Text = "t" }, CancellationToken.None
        );

        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Is<RabbitMQ.Client.BasicProperties>(x => x.DeliveryMode == default),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>()
        );
    }
}
