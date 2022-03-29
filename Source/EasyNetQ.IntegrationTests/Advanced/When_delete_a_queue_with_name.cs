using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_delete_a_queue_with_name : IDisposable
{
    private readonly IBus bus;

    public When_delete_a_queue_with_name(RabbitMQFixture rmqFixture)
    {
        bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");
    }

    [Fact]
    public async Task Should_delete_existing_queue()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queueName, cts.Token);

        await bus.Advanced.QueueDeclarePassiveAsync(queueName, cts.Token);
        await bus.Advanced.QueueDeleteAsync(queueName, cancellationToken: cts.Token);

        var exception = Assert.Throws<OperationInterruptedException>(() => bus.Advanced.QueueDeclarePassive(queueName, cts.Token));
        Assert.Equal(404, exception.ShutdownReason.ReplyCode);
    }

    public void Dispose() => bus.Dispose();
}
