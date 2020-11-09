using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests
{
    public class NonGenericPubSubExtensionsTests
    {
        private readonly Action<IPublishConfiguration> configure = c => { };
        private readonly IPubSub pubSub;

        public NonGenericPubSubExtensionsTests()
        {
            pubSub = Substitute.For<IPubSub>();
        }

        [Fact]
        public async Task Should_be_able_to_publish_struct()
        {
            var message = DateTime.UtcNow;
            var messageType = typeof(DateTime);
            await pubSub.PublishAsync(message, messageType, configure);

#pragma warning disable 4014
            pubSub.Received()
                .PublishAsync(
                    Arg.Is(message),
                    Arg.Is(configure),
                    Arg.Any<CancellationToken>()
                );
#pragma warning restore 4014
        }

        [Fact]
        public async Task Should_be_able_to_publish()
        {
            var message = new Dog();
            var messageType = typeof(Dog);

            await pubSub.PublishAsync(message, messageType, configure);

#pragma warning disable 4014
            pubSub.Received()
                .PublishAsync(
                    Arg.Is(message),
                    Arg.Is(configure),
                    Arg.Any<CancellationToken>()
                );
#pragma warning restore 4014
        }

        [Fact]
        public async Task Should_be_able_to_publish_polymorphic()
        {
            var message = (IAnimal) new Dog();
            var messageType = typeof(IAnimal);

            await pubSub.PublishAsync(message, messageType, configure);

#pragma warning disable 4014
            pubSub.Received()
                .PublishAsync(
                    Arg.Is(message),
                    Arg.Is(configure),
                    Arg.Any<CancellationToken>()
                );
#pragma warning restore 4014
        }
    }
}
