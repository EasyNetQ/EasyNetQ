using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_consumer_with_auto_ack : IDisposable
{
    private readonly SelfHostedBus bus;

    public When_consumer_with_auto_ack(RabbitMQFixture rmqFixture)
    {
        bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");
    }

    [Fact]
    public async Task Should_consume_with_auto_ack()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queueName, cancellationToken: cts.Token);

        var allMessagesReceived = new AsyncCountdownEvent();

        for (var i = 0; i < 10; ++i)
        {
            await bus.Advanced.PublishAsync(
                Exchange.Default, queueName, true, true, MessageProperties.Empty, ReadOnlyMemory<byte>.Empty, cts.Token
            );
            allMessagesReceived.Increment();
        }

        using (
            bus.Advanced.Consume(
                queue,
                (_, _, _) => allMessagesReceived.Decrement(),
                c => c.WithAutoAck()
            )
        )
            await allMessagesReceived.WaitAsync(cts.Token);
    }

    public void Dispose() => bus.Dispose();
}
