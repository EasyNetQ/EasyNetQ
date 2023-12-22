using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_consumer_callback_does_not_respect_ct : IDisposable
{
    private readonly IBus bus;

    public When_consumer_callback_does_not_respect_ct(RabbitMQFixture rmqFixture)
    {
        bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1");
    }

    [Fact]
    public async Task Should_be_able_to_shutdown()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var allMessagesReceived = new AsyncCountdownEvent();

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queueName, cancellationToken: cts.Token);

        await bus.Advanced.PublishAsync(
            Exchange.Default, queueName, true, new MessageProperties(), ReadOnlyMemory<byte>.Empty, cts.Token
        );
        allMessagesReceived.Increment();

        using (
            bus.Advanced.Consume(queue, (_, _, _) =>
            {
                allMessagesReceived.Decrement();
                return Task.Delay(-1, CancellationToken.None);
            })
        )
            allMessagesReceived.Wait();
    }

    public void Dispose() => bus.Dispose();
}
