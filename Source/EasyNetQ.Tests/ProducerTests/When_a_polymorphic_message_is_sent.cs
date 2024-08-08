using System.Text;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_polymorphic_message_is_sent : IDisposable
{
    private readonly MockBuilder mockBuilder;
    private const string interfaceTypeName = "EasyNetQ.Tests.ProducerTests.IMyMessageInterface, EasyNetQ.Tests";
    private const string implementationTypeName = "EasyNetQ.Tests.ProducerTests.MyImplementation, EasyNetQ.Tests";

    public When_a_polymorphic_message_is_sent()
    {
        mockBuilder = new MockBuilder();

        var message = new MyImplementation
        {
            Text = "Hello Polymorphs!",
            NotInInterface = "Hi"
        };

        mockBuilder.PubSub.Publish<IMyMessageInterface>(message);
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public async Task Should_name_exchange_after_interface()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync(
            Arg.Is(interfaceTypeName),
            Arg.Is("topic"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object>>()
        );
    }

    [Fact]
    public async Task Should_publish_to_correct_exchange()
    {
        await mockBuilder.Channels[1].Received().BasicPublishAsync(
            Arg.Is(interfaceTypeName),
            Arg.Is(""),
            Arg.Is<RabbitMQ.Client.BasicProperties>(x => x.Type == implementationTypeName),
            Arg.Is<ReadOnlyMemory<byte>>(
                x => x.ToArray().SequenceEqual(
                    Encoding.UTF8.GetBytes("{\"Text\":\"Hello Polymorphs!\",\"NotInInterface\":\"Hi\"}")
                )
            ),
            Arg.Is(false),
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
