using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.PubSub
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_with_default_options : IDisposable
    {
        public When_publish_and_subscribe_with_default_options(RabbitMQFixture fixture)
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
        public async Task Should_publish_and_consume()
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var subscriptionId = Guid.NewGuid().ToString();
            var messagesSink = new MessagesSink(MessagesCount);
            var messages = MessagesFactories.Create(MessagesCount);

            using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive))
            {
                await bus.PubSub.PublishBatchAsync(messages, timeoutCts.Token).ConfigureAwait(false);

                await messagesSink.WaitAllReceivedAsync(timeoutCts.Token).ConfigureAwait(false);
                messagesSink.ReceivedMessages.Should().Equal(messages);
            }
        }

        [Fact]
        public async Task Should_publish_and_consume_with_multiple_subscription_ids()
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var firstConsumerMessagesSink = new MessagesSink(MessagesCount);
            var secondConsumerMessagesSink = new MessagesSink(MessagesCount);
            var messages = MessagesFactories.Create(MessagesCount);

            using (
                await bus.PubSub.SubscribeAsync<Message>(
                    Guid.NewGuid().ToString(), firstConsumerMessagesSink.Receive, timeoutCts.Token
                )
            )
            using (
                await bus.PubSub.SubscribeAsync<Message>(
                    Guid.NewGuid().ToString(), secondConsumerMessagesSink.Receive, timeoutCts.Token
                )
            )
            {
                await bus.PubSub.PublishBatchAsync(messages, timeoutCts.Token).ConfigureAwait(false);

                await Task.WhenAll(
                    firstConsumerMessagesSink.WaitAllReceivedAsync(timeoutCts.Token),
                    secondConsumerMessagesSink.WaitAllReceivedAsync(timeoutCts.Token)
                ).ConfigureAwait(false);

                firstConsumerMessagesSink.ReceivedMessages.Should().BeEquivalentTo(messages);
                secondConsumerMessagesSink.ReceivedMessages.Should().BeEquivalentTo(messages);
            }
        }

        [Fact]
        public async Task Should_publish_and_consume_with_same_subscription_ids()
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var subscriptionId = Guid.NewGuid().ToString();
            var messagesSink = new MessagesSink(MessagesCount);
            var messages = MessagesFactories.Create(MessagesCount);

            using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, timeoutCts.Token))
            using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, timeoutCts.Token))
            using (await bus.PubSub.SubscribeAsync<Message>(subscriptionId, messagesSink.Receive, timeoutCts.Token))
            {
                await bus.PubSub.PublishBatchAsync(messages, timeoutCts.Token).ConfigureAwait(false);

                await messagesSink.WaitAllReceivedAsync(timeoutCts.Token).ConfigureAwait(false);
                messagesSink.ReceivedMessages.Should().BeEquivalentTo(messages);
            }
        }
    }
}
