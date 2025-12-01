using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_polymorphic_message_is_delivered_to_the_consumer : IDisposable
{
    private readonly MockBuilder mockBuilder;
    private ITestMessageInterface receivedMessage;

    public When_a_polymorphic_message_is_delivered_to_the_consumer()
    {
        mockBuilder = new MockBuilder();

        var queue = new Queue("test_queue", false);

        mockBuilder.Bus.Advanced.Consume<ITestMessageInterface>(queue, (message, _) => receivedMessage = message.Body);

        var publishedMessage = new Implementation { Text = "Hello Polymorphs!" };
        var serializedMessage = new ReflectionBasedNewtonsoftJsonSerializer().MessageToBytes(typeof(Implementation), publishedMessage);
        var properties = new BasicProperties
        {
            Type = new DefaultTypeNameSerializer().Serialize(typeof(Implementation))
        };

        mockBuilder.Consumers[0].HandleBasicDeliver(
            "consumer_tag",
            0,
            false,
            "exchange",
            "routing_key",
            properties,
            serializedMessage.Memory
        ).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public void Should_correctly_deserialize_message()
    {
        receivedMessage.Should().NotBeNull();
        receivedMessage.Should().BeOfType<Implementation>();
        receivedMessage.Text.Should().Be("Hello Polymorphs!");
    }
}

public interface ITestMessageInterface
{
    string Text { get; set; }
}

public class Implementation : ITestMessageInterface
{
    public string Text { get; set; }
}
