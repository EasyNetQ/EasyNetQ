using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests;

public class When_publish_is_called : IDisposable
{
    private const string correlationId = "abc123";

    private readonly MockBuilder mockBuilder;

    public When_publish_is_called()
    {
        mockBuilder = new MockBuilder(x =>
            x.AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId))
        );

        var message = new MyMessage { Text = "Hiya!" };
        mockBuilder.PubSub.Publish(message);
        WaitForMessageToPublish();
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    private void WaitForMessageToPublish()
    {
        var autoResetEvent = new AutoResetEvent(false);
        mockBuilder.EventBus.Subscribe((in PublishedMessageEvent _) => autoResetEvent.Set());
        autoResetEvent.WaitOne(1000);
    }

    [Fact]
    public void Should_create_a_channel_to_publish_on()
    {
        // a channel is also created then disposed to declare the exchange.
        mockBuilder.Channels.Count.Should().Be(2);
    }

    [Fact]
    public async Task Should_call_basic_publish()
    {
        await mockBuilder.Channels[1].Received().BasicPublishAsync(
            Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
            Arg.Is(""),
            Arg.Is<RabbitMQ.Client.BasicProperties>(
                x => x.CorrelationId == correlationId
                     && x.Type == "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"
                     && x.DeliveryMode == (DeliveryModes)2
            ),
            Arg.Is<ReadOnlyMemory<byte>>(
                x => x.ToArray().SequenceEqual(Encoding.UTF8.GetBytes("{\"Text\":\"Hiya!\"}"))
            ),
            Arg.Is(false),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_declare_exchange()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync(
            Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
            Arg.Is("topic"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Is((IDictionary<string, object>)null)
        );
    }
}

public class When_publish_with_topic_is_called : IDisposable
{
    private readonly MockBuilder mockBuilder;

    public When_publish_with_topic_is_called()
    {
        mockBuilder = new MockBuilder();

        var message = new MyMessage { Text = "Hiya!" };
        mockBuilder.PubSub.Publish(message, c => c.WithTopic("X.A"));
        WaitForMessageToPublish();
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    private void WaitForMessageToPublish()
    {
        var autoResetEvent = new AutoResetEvent(false);
        mockBuilder.EventBus.Subscribe((in PublishedMessageEvent _) => autoResetEvent.Set());
        autoResetEvent.WaitOne(1000);
    }

    [Fact]
    public async Task Should_call_basic_publish_with_correct_routing_key()
    {
        await mockBuilder.Channels[1].Received().BasicPublishAsync(
            Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
            Arg.Is("X.A"),
            Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Is(false),
            Arg.Any<CancellationToken>()
        );
    }
}
