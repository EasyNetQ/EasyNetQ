using System.Text;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_polymorphic_message_is_sent : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;
    private const string interfaceTypeName = "EasyNetQ.Tests.ProducerTests.IMyMessageInterface, EasyNetQ.Tests";
    private const string implementationTypeName = "EasyNetQ.Tests.ProducerTests.MyImplementation, EasyNetQ.Tests";

    public When_a_polymorphic_message_is_sent()
    {
        mockBuilder = new MockBuilder();

    }

    public async Task InitializeAsync()
    {
        var message = new MyImplementation
        {
            Text = "Hello Polymorphs!",
            NotInInterface = "Hi"
        };
        await mockBuilder.PubSub.PublishAsync<IMyMessageInterface>(message);
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public async Task Should_name_exchange_after_interface()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync(
            Arg.Is(interfaceTypeName),
            Arg.Is("topic"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Should_publish_to_correct_exchange()
    {
        await mockBuilder.Channels[1].Received().BasicPublishAsync(
            Arg.Is(interfaceTypeName),
            Arg.Is(""),
            Arg.Is(false),
            Arg.Is<RabbitMQ.Client.BasicProperties>(x => x.Type == implementationTypeName),
            Arg.Is<ReadOnlyMemory<byte>>(
                x => x.ToArray().SequenceEqual(
                    Encoding.UTF8.GetBytes("{\"Text\":\"Hello Polymorphs!\",\"NotInInterface\":\"Hi\"}")
                )
            ),
            Arg.Any<CancellationToken>()
        );
    }
}

public interface IMyMessageInterface
{
    string Text { get; set; }
}

public class MyImplementation : IMyMessageInterface
{
    public string Text { get; set; }
    public string NotInInterface { get; set; }
}
