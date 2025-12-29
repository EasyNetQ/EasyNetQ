using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.SendReceive;

[Collection("RabbitMQ")]
public class When_send_receive_multiple_message_types : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_send_receive_multiple_message_types(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    public async Task DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }

    private const int MessagesCount = 10;

    [Fact]
    public async Task Test()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queue = Guid.NewGuid().ToString();
        var bunniesSink = new MessagesSink(MessagesCount);
        var rabbitsSink = new MessagesSink(MessagesCount);
        var bunnies = MessagesFactories.Create(MessagesCount, i => new BunnyMessage(i));
        var rabbits = MessagesFactories.Create(MessagesCount, i => new RabbitMessage(i));
        await using (
            await bus.SendReceive.ReceiveAsync(
                queue,
                x => x.Add<BunnyMessage>(bunniesSink.Receive).Add<RabbitMessage>(rabbitsSink.Receive),
                cts.Token
            )
        )
        {
            await bus.SendReceive.SendBatchAsync(queue, bunnies, cts.Token);
            await bus.SendReceive.SendBatchAsync(queue, rabbits, cts.Token);

            await Task.WhenAll(
                bunniesSink.WaitAllReceivedAsync(cts.Token),
                rabbitsSink.WaitAllReceivedAsync(cts.Token)
            );

            bunniesSink.ReceivedMessages.Should().Equal(bunnies);
            rabbitsSink.ReceivedMessages.Should().Equal(rabbits);
        }
    }
}
