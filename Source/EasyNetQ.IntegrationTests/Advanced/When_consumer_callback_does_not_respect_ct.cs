using EasyNetQ.Internals;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

#pragma warning disable IDISP006
[Collection("RabbitMQ")]
public class When_consumer_callback_does_not_respect_ct : IAsyncLifetime
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

        using var allMessagesReceived = new AsyncCountdownEvent();

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queueName, cancellationToken: cts.Token);

        await bus.Advanced.PublishAsync(
            Exchange.Default, queueName, true, true, MessageProperties.Empty, ReadOnlyMemory<byte>.Empty, cts.Token
        );
        allMessagesReceived.Increment();

        await using (
            await bus.Advanced.ConsumeAsync(queue, (_, _, _) =>
            {
                allMessagesReceived.Decrement();
                return Task.Delay(-1, CancellationToken.None);
            })
        )
            await allMessagesReceived.WaitAsync(cts.Token);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }
}
