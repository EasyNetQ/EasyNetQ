using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests
{
    public class NonGenericPubSubExtensionsTests
    {
        private readonly Action<IPublishConfiguration> publishConfigure = _ => { };
        private readonly Action<ISubscriptionConfiguration> subscribeConfigure = _ => { };
        private readonly IPubSub pubSub;
        private readonly AwaitableDisposable<SubscriptionResult> subscribeResult;

        public NonGenericPubSubExtensionsTests()
        {
            pubSub = Substitute.For<IPubSub>();

            var exchange = new Exchange("test");
            var queue = new Queue("test");

            subscribeResult = new AwaitableDisposable<SubscriptionResult>(
                Task.FromResult(
                    new SubscriptionResult(exchange, queue, DisposableAction.Create(_ => { }, 42))
                )
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
            pubSub.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<Func<Dog, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>(),
                Arg.Any<CancellationToken>()
            ).ReturnsForAnyArgs(subscribeResult);
            using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.FromResult(AckStrategies.Ack), subscribeConfigure);
#pragma warning disable 4014
            pubSub.Received()
                .SubscribeAsync(Arg.Is("Id"), Arg.Any<Func<Dog, CancellationToken, Task>>(), Arg.Is(subscribeConfigure), Arg.Any<CancellationToken>());
#pragma warning restore 4014
        }

        [Fact]
        public async Task Should_be_able_to_subscribe_polymorphic()
        {
            var messageType = typeof(IAnimal);
            pubSub.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<Func<IAnimal, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>(),
                Arg.Any<CancellationToken>()
            ).ReturnsForAnyArgs(subscribeResult);

            using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.FromResult(AckStrategies.Ack), subscribeConfigure);
#pragma warning disable 4014
            pubSub.Received()
                .SubscribeAsync(Arg.Is("Id"), Arg.Any<Func<IAnimal, CancellationToken, Task>>(), Arg.Is(subscribeConfigure), Arg.Any<CancellationToken>());
#pragma warning restore 4014
        }

        [Fact]
        public async Task Should_be_able_to_subscribe_struct()
        {
            var messageType = typeof(DateTime);

            pubSub.SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<Func<DateTime, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>(),
                Arg.Any<CancellationToken>()
            ).ReturnsForAnyArgs(subscribeResult);

            using var _ = await pubSub.SubscribeAsync("Id", messageType, (_, _, _) => Task.FromResult(AckStrategies.Ack), subscribeConfigure);
#pragma warning disable 4014
            pubSub.Received()
                .SubscribeAsync(Arg.Is("Id"), Arg.Any<Func<DateTime, CancellationToken, Task>>(), Arg.Is(subscribeConfigure), Arg.Any<CancellationToken>());
#pragma warning restore 4014
        }
    }
}
