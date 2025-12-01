namespace EasyNetQ.Tests;

public class NonGenericSendReceiveExtensionsTests
{
    private readonly Action<ISendConfiguration> configure = _ => { };
    private readonly ISendReceive sendReceive = Substitute.For<ISendReceive>();
    private const string Queue = "queue";

    [Fact]
    public async Task Should_be_able_to_send_struct()
    {
        var message = DateTime.UtcNow;
        var messageType = typeof(DateTime);
        await sendReceive.SendAsync(Queue, message, messageType, configure);

#pragma warning disable 4014
        sendReceive.Received()
            .SendAsync(
                Arg.Is(Queue),
                Arg.Is(message),
                Arg.Is(configure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }

    [Fact]
    public async Task Should_be_able_to_send()
    {
        var message = new Dog();
        var messageType = typeof(Dog);

        await sendReceive.SendAsync(Queue, message, messageType, configure);

#pragma warning disable 4014
        sendReceive.Received()
            .SendAsync(
                Arg.Is(Queue),
                Arg.Is(message),
                Arg.Is(configure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }

    [Fact]
    public async Task Should_be_able_to_publish_polymorphic()
    {
        var message = (IAnimal)new Dog();
        var messageType = typeof(IAnimal);

        await sendReceive.SendAsync(Queue, message, messageType, configure);

#pragma warning disable 4014
        sendReceive.Received()
            .SendAsync(
                Arg.Is(Queue),
                Arg.Is(message),
                Arg.Is(configure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }
}
