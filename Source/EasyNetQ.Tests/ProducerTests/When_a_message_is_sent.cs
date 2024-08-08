using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_message_is_sent : IDisposable
{
    private readonly MockBuilder mockBuilder;
    private const string queueName = "the_queue_name";

    public When_a_message_is_sent()
    {
        mockBuilder = new MockBuilder();

        mockBuilder.SendReceive.Send(queueName, new MyMessage { Text = "Hello World" });
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public async Task Should_publish_the_message()
    {
        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            Arg.Is(""),
            Arg.Is(queueName),
            Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Is(false),
            Arg.Any<CancellationToken>()
        );
    }
}
