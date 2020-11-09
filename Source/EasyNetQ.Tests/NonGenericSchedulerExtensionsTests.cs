using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests
{
    public class NonGenericSchedulerExtensionsTests
    {
        private readonly Action<IFuturePublishConfiguration> configure = c => { };
        private readonly TimeSpan delay = TimeSpan.FromSeconds(42);
        private readonly IScheduler scheduler;

        public NonGenericSchedulerExtensionsTests()
        {
            scheduler = Substitute.For<IScheduler>();
        }

        [Fact]
        public async Task Should_be_able_to_future_publish_struct()
        {
            var message = DateTime.UtcNow;
            var messageType = typeof(DateTime);
            await scheduler.FuturePublishAsync(message, messageType, delay, configure);

#pragma warning disable 4014
            scheduler.Received()
                .FuturePublishAsync(
                    Arg.Is(message),
                    Arg.Is(delay),
                    Arg.Is(configure),
                    Arg.Any<CancellationToken>()
                );
#pragma warning restore 4014
        }

        [Fact]
        public async Task Should_be_able_to_future_publish()
        {
            var message = new Dog();
            var messageType = typeof(Dog);

            await scheduler.FuturePublishAsync(message, messageType, delay, configure);

#pragma warning disable 4014
            scheduler.Received()
                .FuturePublishAsync(
                    Arg.Is(message),
                    Arg.Is(delay),
                    Arg.Is(configure),
                    Arg.Any<CancellationToken>()
                );
#pragma warning restore 4014
        }

        [Fact]
        public async Task Should_be_able_to_future_publish_polymorphic()
        {
            var message = (IAnimal) new Dog();
            var messageType = typeof(IAnimal);

            await scheduler.FuturePublishAsync(message, messageType, delay, configure);

#pragma warning disable 4014
            scheduler.Received()
                .FuturePublishAsync(
                    Arg.Is(message),
                    Arg.Is(delay),
                    Arg.Is(configure),
                    Arg.Any<CancellationToken>()
                );
#pragma warning restore 4014
        }
    }
}
