using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_message_is_sent : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;
    private const string queueName = "the_queue_name";
    private const string correlationId = "abc123";

    public When_a_message_is_sent()
    {
        mockBuilder = new MockBuilder(x =>
            x.AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId))
        );
    }

    public async ValueTask InitializeAsync() => await mockBuilder.SendReceive.SendAsync(queueName, new MyMessage { Text = "Hello World" });

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_publish_the_message()
    {
        mockBuilder.PublishedOn(mockBuilder.Channels[0]).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage(
                "",
                queueName,
                false,
                new RabbitMQ.Client.BasicProperties
                {
                    CorrelationId = correlationId,
                    Type = "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests",
                    DeliveryMode = DeliveryModes.Persistent
                },
                "{\"Text\":\"Hello World\"}"u8.ToArray()
            )
        );
    }
}
