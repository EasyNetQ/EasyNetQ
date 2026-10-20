using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests;

public class When_publish_is_called : IAsyncLifetime
{
    private const string correlationId = "abc123";

    private readonly MockBuilder mockBuilder;

    public When_publish_is_called()
    {
        mockBuilder = new MockBuilder(x =>
            x.AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId))
        );
    }

    public async ValueTask InitializeAsync()
    {
        var message = new MyMessage { Text = "Hiya!" };
        await mockBuilder.PubSub.PublishAsync(message);
        WaitForMessageToPublish();
    }

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private void WaitForMessageToPublish()
    {
        using var autoResetEvent = new AutoResetEvent(false);
        mockBuilder.Published += () => autoResetEvent.Set();
        autoResetEvent.WaitOne(1000);
    }

    [Fact]
    public void Should_create_a_channel_to_publish_on()
    {
        // a channel is also created then disposed to declare the exchange.
        mockBuilder.Channels.Count.Should().Be(2);
    }

    [Fact]
    public void Should_call_basic_publish()
    {
        mockBuilder.PublishedOn(mockBuilder.Channels[1]).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage(
                "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests",
                "",
                false,
                new RabbitMQ.Client.BasicProperties
                {
                    CorrelationId = correlationId,
                    Type = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests",
                    DeliveryMode = DeliveryModes.Persistent
                },
                Encoding.UTF8.GetBytes("{\"Text\":\"Hiya!\"}")
            )
        );
    }

    [Fact]
    public async Task Should_declare_exchange()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync(
            Arg.Is("EasyNetQ.Tests.MyMessage, EasyNetQ.Tests"),
            Arg.Is(ExchangeType.Topic),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Is((IDictionary<string, object?>?)null),
            cancellationToken: Arg.Any<CancellationToken>()
        );
    }
}

public class When_publish_with_topic_is_called : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;

    private const string correlationId = "abc123";

    public When_publish_with_topic_is_called()
    {
        mockBuilder = new MockBuilder(x =>
            x.AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId))
        );
    }

    public async ValueTask InitializeAsync()
    {
        var message = new MyMessage { Text = "Hiya!" };
        await mockBuilder.PubSub.PublishAsync(message, c => c.WithTopic("X.A"));
        WaitForMessageToPublish();
    }

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private void WaitForMessageToPublish()
    {
        using var autoResetEvent = new AutoResetEvent(false);
        mockBuilder.Published += () => autoResetEvent.Set();
        autoResetEvent.WaitOne(1000);
    }

    [Fact]
    public void Should_call_basic_publish_with_correct_routing_key()
    {
        mockBuilder.PublishedOn(mockBuilder.Channels[1]).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage(
                "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests",
                "X.A",
                false,
                new RabbitMQ.Client.BasicProperties
                {
                    CorrelationId = correlationId,
                    Type = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests",
                    DeliveryMode = DeliveryModes.Persistent
                },
                Encoding.UTF8.GetBytes("{\"Text\":\"Hiya!\"}")
            )
        );
    }
}
