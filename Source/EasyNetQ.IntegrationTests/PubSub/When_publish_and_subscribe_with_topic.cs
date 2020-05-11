using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.PubSub
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_with_topic : IDisposable
    {
        public When_publish_and_subscribe_with_topic(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private const int MessagesCount = 10;

        private readonly IBus bus;

        [Fact]
        public async Task Test()
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var firstTopicMessagesSink = new MessagesSink(MessagesCount);
            var secondTopicMessagesSink = new MessagesSink(MessagesCount);

            var firstTopicMessages = MessagesFactories.Create(MessagesCount);
            var secondTopicMessages = MessagesFactories.Create(MessagesCount, MessagesCount);

            using (
                await bus.PubSub.SubscribeAsync<Message>(
                    Guid.NewGuid().ToString(),
                    firstTopicMessagesSink.Receive,
                    x => x.WithTopic("first"),
                    timeoutCts.Token
                )
            )
            using (
                await bus.PubSub.SubscribeAsync<Message>(
                    Guid.NewGuid().ToString(),
                    secondTopicMessagesSink.Receive,
                    x => x.WithTopic("second"),
                    timeoutCts.Token
                )
            )
            {
                await bus.PubSub.PublishBatchAsync(
                    firstTopicMessages, x => x.WithTopic("first"), timeoutCts.Token
                ).ConfigureAwait(false);
                await bus.PubSub.PublishBatchAsync(
                    secondTopicMessages, x => x.WithTopic("second"), timeoutCts.Token
                ).ConfigureAwait(false);

                await Task.WhenAll(
                    firstTopicMessagesSink.WaitAllReceivedAsync(timeoutCts.Token),
                    secondTopicMessagesSink.WaitAllReceivedAsync(timeoutCts.Token)
                ).ConfigureAwait(false);

                firstTopicMessagesSink.ReceivedMessages.Should().Equal(firstTopicMessages);
                secondTopicMessagesSink.ReceivedMessages.Should().Equal(secondTopicMessages);
            }
        }
    }
}
