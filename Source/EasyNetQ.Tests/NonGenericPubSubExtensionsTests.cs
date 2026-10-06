using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests;

public class NonGenericPubSubExtensionsTests
{
    private readonly Action<IPublishConfiguration> publishConfigure = _ => { };
    private readonly Action<ISubscriptionConfiguration> subscribeConfigure = _ => { };
    private readonly IPubSub pubSub;
    private readonly Task<SubscriptionResult> subscribeResult;

    private sealed class NoOpAsynDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }

    public NonGenericPubSubExtensionsTests()
    {
        pubSub = Substitute.For<IPubSub>();

        var exchange = new Exchange("test");
        var queue = new Queue("test");

        subscribeResult = Task.FromResult(
#pragma warning disable IDISP004
            new SubscriptionResult(exchange, queue, new NoOpAsynDisposable())
#pragma warning restore IDISP004
        );
    }

    [Fact]
    public async Task Should_be_able_to_publish_struct()
    {
        var message = DateTime.UtcNow;
        var messageType = typeof(DateTime);
        await pubSub.PublishAsync(message, messageType, publishConfigure, cancellationToken: CancellationToken.None);

#pragma warning disable 4014
        pubSub.Received()
            .PublishAsync(
                Arg.Is(message),
                Arg.Is(publishConfigure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }

    [Fact]
    public async Task Should_be_able_to_publish()
    {
        var message = new Dog();
        var messageType = typeof(Dog);

        await pubSub.PublishAsync(message, messageType, publishConfigure, cancellationToken: CancellationToken.None);

#pragma warning disable 4014
        pubSub.Received()
            .PublishAsync(
                Arg.Is(message),
                Arg.Is(publishConfigure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }

    [Fact]
    public async Task Should_be_able_to_publish_polymorphic()
    {
        var message = (IAnimal)new Dog();
        var messageType = typeof(IAnimal);

        await pubSub.PublishAsync(message, messageType, publishConfigure, cancellationToken: CancellationToken.None);

#pragma warning disable 4014
        pubSub.Received()
            .PublishAsync(
                Arg.Is(message),
                Arg.Is(publishConfigure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }

    [Fact]
    public async Task Should_be_able_to_subscribe()
    {
        var messageType = typeof(Dog);
#pragma warning disable IDISP004
        pubSub.SubscribeAsync(
#pragma warning restore IDISP004
            Arg.Any<string>(),
            Arg.Any<Func<Dog, CancellationToken, Task>>(),
            Arg.Any<Action<ISubscriptionConfiguration>>(),
            Arg.Any<CancellationToken>()
        ).ReturnsForAnyArgs(subscribeResult);
        await using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.CompletedTask, subscribeConfigure, cancellationToken: CancellationToken.None);
        await pubSub.Received()
            .SubscribeAsync(Arg.Is("Id"), Arg.Any<Func<Dog, CancellationToken, Task>>(), Arg.Is(subscribeConfigure), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_be_able_to_subscribe_polymorphic()
    {
        var messageType = typeof(IAnimal);
#pragma warning disable IDISP004
        pubSub.SubscribeAsync(
#pragma warning restore IDISP004
            Arg.Any<string>(),
            Arg.Any<Func<IAnimal, CancellationToken, Task>>(),
            Arg.Any<Action<ISubscriptionConfiguration>>(),
            Arg.Any<CancellationToken>()
        ).ReturnsForAnyArgs(subscribeResult);

        await using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.CompletedTask, subscribeConfigure, cancellationToken: CancellationToken.None);
        await pubSub.Received()
            .SubscribeAsync(Arg.Is("Id"), Arg.Any<Func<IAnimal, CancellationToken, Task>>(), Arg.Is(subscribeConfigure), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_be_able_to_subscribe_struct()
    {
        var messageType = typeof(DateTime);

#pragma warning disable IDISP004
        pubSub.SubscribeAsync(
#pragma warning restore IDISP004
            Arg.Any<string>(),
            Arg.Any<Func<DateTime, CancellationToken, Task>>(),
            Arg.Any<Action<ISubscriptionConfiguration>>(),
            Arg.Any<CancellationToken>()
        ).ReturnsForAnyArgs(subscribeResult);

        await using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.CompletedTask, subscribeConfigure, cancellationToken: CancellationToken.None);
        await pubSub.Received()
            .SubscribeAsync(Arg.Is("Id"), Arg.Any<Func<DateTime, CancellationToken, Task>>(), Arg.Is(subscribeConfigure), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_pass_a_null_message_with_the_subscribed_type()
    {
        Func<Dog, CancellationToken, Task>? typedHandler = null;
        pubSub.SubscribeAsync(Arg.Any<string>(), Arg.Do<Func<Dog, CancellationToken, Task>>(h => typedHandler = h), Arg.Any<Action<ISubscriptionConfiguration>>(), Arg.Any<CancellationToken>())
            .Returns(subscribeResult);
        object? received = new();
        Type? receivedType = null;

        await using var _ = await pubSub.SubscribeAsync(
            "Id", typeof(Dog), (message, type, _) =>
            {
                received = message;
                receivedType = type;
                return Task.CompletedTask;
            }, subscribeConfigure, cancellationToken: CancellationToken.None
        );
        Assert.NotNull(typedHandler);
        await typedHandler(null!, CancellationToken.None);

        received.Should().BeNull();
        receivedType.Should().Be<Dog>();
    }
}
