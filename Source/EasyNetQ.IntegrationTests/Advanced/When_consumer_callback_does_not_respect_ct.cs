using EasyNetQ.Internals;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_consumer_callback_does_not_respect_ct : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_consumer_callback_does_not_respect_ct(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    [Fact]
    public async Task Should_be_able_to_shutdown()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var allMessagesReceived = new AsyncCountdownEvent();

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queueName, cancellationToken: cts.Token);

        await bus.Advanced.PublishAsync(
            Exchange.Default, queueName, true, true, MessageProperties.Empty, ReadOnlyMemory<byte>.Empty, cts.Token
        );
        allMessagesReceived.Increment();

        using (
            bus.Advanced.Consume(queue, (_, _, _) =>
            {
                allMessagesReceived.Decrement();
                return Task.Delay(-1, CancellationToken.None);
            })
        )
            await allMessagesReceived.WaitAsync(cts.Token);
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
