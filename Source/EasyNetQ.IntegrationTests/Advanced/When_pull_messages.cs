using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Advanced
{
    [Collection("RabbitMQ")]
    public class When_pull_messages : IDisposable
    {
        public When_pull_messages(RabbitMQFixture rmqFixture)
        {
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private readonly IBus bus;

        [Fact]
        public async Task Should_be_able_ack()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queue = await bus.Advanced.QueueDeclareAsync(
                Guid.NewGuid().ToString("N"), cts.Token
            );
            await bus.Advanced.PublishAsync(
                Exchange.GetDefault(), queue.Name, false, new MessageProperties(), Array.Empty<byte>(), cts.Token
            );

            var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

            {
                var pullResult = await consumer.PullAsync(cts.Token);
                pullResult.IsAvailable.Should().BeTrue();
                await consumer.AckAsync(
                    pullResult.ReceivedInfo.DeliveryTag, cts.Token
                );
            }

            {
                var pullResult = await consumer.PullAsync(cts.Token);
                pullResult.IsAvailable.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Should_be_able_reject()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queue = await bus.Advanced.QueueDeclareAsync(
                Guid.NewGuid().ToString("N"), cts.Token
            );
            await bus.Advanced.PublishAsync(
                Exchange.GetDefault(), queue.Name, false, new MessageProperties(), Array.Empty<byte>(), cts.Token
            );

            var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

            {
                var pullResult = await consumer.PullAsync(cts.Token);
                pullResult.IsAvailable.Should().BeTrue();
                await consumer.RejectAsync(
                    pullResult.ReceivedInfo.DeliveryTag, false, cts.Token
                );
            }

            {
                var pullResult = await consumer.PullAsync(cts.Token);
                pullResult.IsAvailable.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Should_be_able_reject_with_requeue()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queue = await bus.Advanced.QueueDeclareAsync(
                Guid.NewGuid().ToString("N"), cts.Token
            );
            await bus.Advanced.PublishAsync(
                Exchange.GetDefault(), queue.Name, false, new MessageProperties(), Array.Empty<byte>(), cts.Token
            );

            var consumer = bus.Advanced.CreatePullingConsumer(queue, false);

            {
                var pullResult = await consumer.PullAsync(cts.Token);
                pullResult.IsAvailable.Should().BeTrue();
                await consumer.RejectAsync(
                    pullResult.ReceivedInfo.DeliveryTag, true, cts.Token
                );
            }

            {
                var pullResult = await consumer.PullAsync(cts.Token);
                pullResult.IsAvailable.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Should_be_able_with_auto_ack()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var queue = await bus.Advanced.QueueDeclareAsync(
                Guid.NewGuid().ToString("N"), cts.Token
            );
            await bus.Advanced.PublishAsync(
                Exchange.GetDefault(), queue.Name, false, new MessageProperties(), Array.Empty<byte>(), cts.Token
            );

            var consumer = bus.Advanced.CreatePullingConsumer(queue);

            {
                var pullResult = await consumer.PullAsync(cts.Token);
                pullResult.IsAvailable.Should().BeTrue();
            }

            {
                var pullResult = await consumer.PullAsync(cts.Token);
                pullResult.IsAvailable.Should().BeFalse();
            }
        }
    }
}
