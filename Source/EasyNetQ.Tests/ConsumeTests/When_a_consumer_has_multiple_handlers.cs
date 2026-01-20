using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_has_multiple_handlers : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;
    private IAnimal animalResult;

    private MyMessage myMessageResult;
    private MyOtherMessage myOtherMessageResult;

    public When_a_consumer_has_multiple_handlers()
    {
        mockBuilder = new MockBuilder();
    }

    public async Task InitializeAsync()
    {
        var queue = new Queue("test_queue", false);

        using var countdownEvent = new CountdownEvent(3);

#pragma warning disable IDISP004
        await mockBuilder.Bus.Advanced.ConsumeAsync(
#pragma warning restore IDISP004
            queue,
            x => x.Add<MyMessage>((message, _) =>
                {
                    myMessageResult = message.Body;
                    countdownEvent.Signal();
                })
                .Add<MyOtherMessage>((message, _) =>
                {
                    myOtherMessageResult = message.Body;
                    countdownEvent.Signal();
                })
                .Add<IAnimal>((message, _) =>
                {
                    animalResult = message.Body;
                    countdownEvent.Signal();
                }));

        await DeliverAsync(new MyMessage { Text = "Hello Polymorphs!" });
        await DeliverAsync(new MyOtherMessage { Text = "Hello Isomorphs!" });
        await DeliverAsync(new Dog());

        if (!countdownEvent.Wait(5000)) throw new TimeoutException();
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private async Task DeliverAsync<T>(T message) where T : class
    {
        using var serializedMessage = new Serialization.SystemTextJson.SystemTextJsonSerializerV2().MessageToBytes(typeof(T), message);
        var properties = new BasicProperties
        {
            Type = new DefaultTypeNameSerializer().Serialize(typeof(T))
        };

        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            "consumer_tag",
            0,
            false,
            "exchange",
            "routing_key",
            properties,
            serializedMessage.Memory
        );
    }

    [Fact]
    public void Should_deliver_a_polymorphic_message()
    {
        animalResult.Should().NotBeNull();
        animalResult.Should().BeOfType<Dog>();
    }

    [Fact]
    public void Should_deliver_myMessage()
    {
        myMessageResult.Should().NotBeNull();
        myMessageResult.Text.Should().Be("Hello Polymorphs!");
    }

    [Fact]
    public void Should_deliver_myOtherMessage()
    {
        myOtherMessageResult.Should().NotBeNull();
        myOtherMessageResult.Text.Should().Be("Hello Isomorphs!");
    }
}
