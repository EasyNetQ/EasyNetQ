using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.PubSub
{
    [Collection("RabbitMQ")]
    public class When_publish_and_subscribe_with_queue_type : IDisposable
    {
        private readonly RabbitMQFixture rmqFixture;

        public When_publish_and_subscribe_with_queue_type(RabbitMQFixture rmqFixture)
        {
            this.rmqFixture = rmqFixture;
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var subscriptionId = Guid.NewGuid().ToString();
            var messagesSink = new MessagesSink(MessagesCount);
            var messages = CreateMessages(MessagesCount);

            using (await bus.PubSub.SubscribeAsync<QuorumQueueMessage>(subscriptionId, messagesSink.Receive))
            {
                await bus.PubSub.PublishBatchAsync(messages, cts.Token);

                await messagesSink.WaitAllReceivedAsync(cts.Token);
                messagesSink.ReceivedMessages.Should().Equal(messages);
            }
        }

        private static List<QuorumQueueMessage> CreateMessages(int count)
        {
            var result = new List<QuorumQueueMessage>();
            for (int i = 0; i < count; i++)
                result.Add(new QuorumQueueMessage(i));
            return result;
        }
    }

    [Queue("QuorumQueue", QueueType = QueueType.Quorum)]
    public class QuorumQueueMessage : Message
    {
        public QuorumQueueMessage(int id) : base(id)
        {
        }
    }
}
