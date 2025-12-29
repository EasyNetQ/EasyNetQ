using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_message_is_sent : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;
    private const string queueName = "the_queue_name";

    public When_a_message_is_sent()
    {
        mockBuilder = new MockBuilder();

    }

    public Task InitializeAsync() => mockBuilder.SendReceive.SendAsync(queueName, new MyMessage { Text = "Hello World" });

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public async Task Should_publish_the_message()
    {
        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            Arg.Is(""),
            Arg.Is(queueName),
            Arg.Is(false),
            Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>()
        );
    }
}
