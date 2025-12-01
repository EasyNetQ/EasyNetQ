using EasyNetQ.Internals;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_consumer_with_auto_ack : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_consumer_with_auto_ack(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    [Fact]
    public async Task Should_consume_with_auto_ack()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queueName, cancellationToken: cts.Token);

        using var allMessagesReceived = new AsyncCountdownEvent();

        for (var i = 0; i < 10; ++i)
        {
            await bus.Advanced.PublishAsync(
                Exchange.Default, queueName, true, true, MessageProperties.Empty, ReadOnlyMemory<byte>.Empty, cts.Token
            );
            allMessagesReceived.Increment();
        }

#warning allMessagesReceived.Decrement
        /*
        await using (
            await bus.Advanced.ConsumeAsync(
                queue,
                (_, _, _) => allMessagesReceived.Decrement(),
                c => c.WithAutoAck(), cancellationToken: cts.Token)
        )
            await allMessagesReceived.WaitAsync(cts.Token);
        */
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }
}
