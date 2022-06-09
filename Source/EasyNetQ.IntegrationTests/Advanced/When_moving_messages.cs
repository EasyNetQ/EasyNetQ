using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_moving_messages : IDisposable
{
    public When_moving_messages(RabbitMQFixture rmqFixture)
    {
        bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;publisherConfirms=True");
    }

    [Fact]
    public async Task Should_be_able_to_move_messages()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var sourceQueue = await bus.Advanced.QueueDeclareAsync(Guid.NewGuid().ToString(), cts.Token);
        for (var i = 0; i < 42; ++i)
            await bus.Advanced.PublishAsync(
                Exchange.Default,
                sourceQueue.Name,
                true,
                new MessageProperties(),
                ReadOnlyMemory<byte>.Empty,
                cts.Token
            );

        var destinationQueue = await bus.Advanced.QueueDeclareAsync(Guid.NewGuid().ToString(), cts.Token);
        await bus.Advanced.MoveMessagesAsync(sourceQueue, destinationQueue, cts.Token);

        var destinationQueueStats = await bus.Advanced.GetQueueStatsAsync(destinationQueue.Name, cts.Token);
        destinationQueueStats.MessagesCount.Should().Be(42);
    }

    public void Dispose() => bus.Dispose();

    private readonly IBus bus;
}
