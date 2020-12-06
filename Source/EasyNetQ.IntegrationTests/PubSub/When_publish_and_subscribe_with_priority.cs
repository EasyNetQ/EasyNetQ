using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.PubSub
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_with_priority : IDisposable
    {
        public When_publish_and_subscribe_with_priority(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=-1");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private const byte LowPriority = 1;
        private const byte HighPriority = 2;
        private const int MessagesCount = 10;

        private readonly IBus bus;

        [Fact]
        public async Task Test()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var messagesSink = new MessagesSink(MessagesCount * 2);
            var highPriorityMessages = MessagesFactories.Create(MessagesCount);
            var lowPriorityMessages = MessagesFactories.Create(MessagesCount, MessagesCount);

            var subscriptionId = Guid.NewGuid().ToString();
            using (
                await bus.PubSub.SubscribeAsync<Message>(
                    subscriptionId, messagesSink.Receive, x => x.WithMaxPriority(2), cts.Token
                )
            )
            {
            }

            await bus.PubSub.PublishBatchAsync(
                lowPriorityMessages, x => x.WithPriority(LowPriority), cts.Token
            );
            await bus.PubSub.PublishBatchAsync(
                highPriorityMessages, x => x.WithPriority(HighPriority), cts.Token
            );

            using (
                await bus.PubSub.SubscribeAsync<Message>(
                    subscriptionId, messagesSink.Receive, x => x.WithMaxPriority(2), cts.Token
                )
            )
            {
                await messagesSink.WaitAllReceivedAsync(cts.Token);

                messagesSink.ReceivedMessages.Should().Equal(highPriorityMessages.Concat(lowPriorityMessages));
            }
        }
    }
}
