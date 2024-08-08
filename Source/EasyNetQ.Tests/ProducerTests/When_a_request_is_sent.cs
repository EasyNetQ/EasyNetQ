using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_request_is_sent : IDisposable
{
    public When_a_request_is_sent()
    {
        var correlationId = Guid.NewGuid().ToString();
        mockBuilder = new MockBuilder(
            c => c.AddSingleton<ICorrelationIdGenerationStrategy>(
                _ => new StaticCorrelationIdGenerationStrategy(correlationId)
            )
        );

        var waiter = new CountdownEvent(2);

        mockBuilder.EventBus.Subscribe((in PublishedMessageEvent _) => waiter.Signal());
        mockBuilder.EventBus.Subscribe((in StartConsumingSucceededEvent _) => waiter.Signal());

        var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
        if (!waiter.Wait(5000))
            throw new TimeoutException();

        DeliverMessage(correlationId).GetAwaiter().GetResult();

        responseMessage = task.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;
    private readonly TestResponseMessage responseMessage;

    private async Task DeliverMessage(string correlationId)
    {
        var properties = new BasicProperties
        {
            Type = "EasyNetQ.Tests.TestResponseMessage, EasyNetQ.Tests",
            CorrelationId = correlationId
        };
        var body = "{ Id:12, Text:\"Hello World\"}"u8.ToArray();

        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            "consumer_tag",
            0,
            false,
            "the_exchange",
            "the_routing_key",
            properties,
            body
        );
    }

    [Fact]
    public async Task Should_declare_the_publish_exchange()
    {
        await mockBuilder.Channels[1].Received().ExchangeDeclareAsync(
            Arg.Is("easy_net_q_rpc"),
            Arg.Is("direct"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object>>()
        );
    }
    [Fact]
    public async Task Should_declare_the_response_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            Arg.Is<string>(arg => arg.StartsWith("easynetq.response.")),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is(true),
            Arg.Any<IDictionary<string, object>>()
        );
    }

    [Fact]
    public async Task Should_publish_request_message()
    {
        await mockBuilder.Channels[3].Received().BasicPublishAsync(
            Arg.Is("easy_net_q_rpc"),
            Arg.Is("EasyNetQ.Tests.TestRequestMessage, EasyNetQ.Tests"),
            Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Is(false),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void Should_return_the_response()
    {
        responseMessage.Text.Should().Be("Hello World");
    }
}
