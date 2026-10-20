using System.Text;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_polymorphic_message_is_sent : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;
    private const string interfaceTypeName = "EasyNetQ.Tests.ProducerTests.IMyMessageInterface, EasyNetQ.Tests";
    private const string implementationTypeName = "EasyNetQ.Tests.ProducerTests.MyImplementation, EasyNetQ.Tests";
    private const string correlationId = "abc123";

    public When_a_polymorphic_message_is_sent()
    {
        mockBuilder = new MockBuilder(x =>
            x.AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId))
        );
    }

    public async ValueTask InitializeAsync()
    {
        var message = new MyImplementation
        {
            Text = "Hello Polymorphs!",
            NotInInterface = "Hi"
        };
        await mockBuilder.PubSub.PublishAsync<IMyMessageInterface>(message);
    }

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public async Task Should_name_exchange_after_interface()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync(
            Arg.Is(interfaceTypeName),
            Arg.Is(ExchangeType.Topic),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void Should_publish_to_correct_exchange()
    {
        mockBuilder.PublishedOn(mockBuilder.Channels[1]).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage(
                interfaceTypeName,
                "",
                false,
                new RabbitMQ.Client.BasicProperties
                {
                    CorrelationId = correlationId,
                    Type = implementationTypeName,
                    DeliveryMode = DeliveryModes.Persistent
                },
                Encoding.UTF8.GetBytes("{\"Text\":\"Hello Polymorphs!\",\"NotInInterface\":\"Hi\"}")
            )
        );
    }
}

public interface IMyMessageInterface
{
    string Text { get; set; }
}

public class MyImplementation : IMyMessageInterface
{
    public string Text { get; set; } = "";
    public string NotInInterface { get; set; } = "";
}
