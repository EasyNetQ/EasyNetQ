using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_consumer_with_auto_ack : IDisposable
{
    private readonly IBus bus;

    public When_consumer_with_auto_ack(RabbitMQFixture rmqFixture)
    {
        bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");
    }

    [Fact]
    public async Task Should_consume_with_auto_ack()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queueName, cts.Token);

        var allMessagesReceived = new AsyncCountdownEvent();

        for (var i = 0; i < 10; ++i)
        {
            await bus.Advanced.PublishAsync(
                Exchange.Default, queueName, true, new MessageProperties(), ReadOnlyMemory<byte>.Empty, cts.Token
            );
            allMessagesReceived.Increment();
        }

        var initialStats = await bus.Advanced.GetQueueStatsAsync(queue, cts.Token);
        initialStats.MessagesCount.Should().Be(10);

        using (
            bus.Advanced.Consume(
                queue,
                (_, _, _) =>
                {
                    allMessagesReceived.Decrement();
                    throw new Exception("Oops");
                },
                c => c.WithAutoAck()
            )
        )
        {
            allMessagesReceived.Wait();
        }

        var finalStats = await bus.Advanced.GetQueueStatsAsync(queue, cts.Token);
        finalStats.MessagesCount.Should().Be(0);
    }

    public void Dispose() => bus.Dispose();
}
