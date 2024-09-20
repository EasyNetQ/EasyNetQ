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

    public NonGenericPubSubExtensionsTests()
    {
        pubSub = Substitute.For<IPubSub>();

        var exchange = new Exchange("test");
        var queue = new Queue("test");

        subscribeResult = Task.FromResult(
#pragma warning disable IDISP004
            new SubscriptionResult(exchange, queue, DisposableAction.Create(_ => { }, 42))
#pragma warning restore IDISP004
        );
    }

    [Fact]
    public async Task Should_be_able_to_publish_struct()
    {
        var message = DateTime.UtcNow;
        var messageType = typeof(DateTime);
        await pubSub.PublishAsync(message, messageType, publishConfigure);

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

        await pubSub.PublishAsync(message, messageType, publishConfigure);

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

        await pubSub.PublishAsync(message, messageType, publishConfigure);

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
        using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.FromResult(AckStrategies.AckAsync), subscribeConfigure);
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

        using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.FromResult(AckStrategies.AckAsync), subscribeConfigure);
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

        using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.FromResult(AckStrategies.AckAsync), subscribeConfigure);
        await pubSub.Received()
            .SubscribeAsync(Arg.Is("Id"), Arg.Any<Func<DateTime, CancellationToken, Task>>(), Arg.Is(subscribeConfigure), Arg.Any<CancellationToken>());
    }
}
