using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.SendReceive;

[Collection("RabbitMQ")]
public class When_send_receive_with_default_options : IDisposable, IAsyncLifetime
{
    private readonly RabbitMQFixture rmqFixture;
    private const int MessagesCount = 10;

    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_send_receive_with_default_options(RabbitMQFixture rmqFixture)
    {
        this.rmqFixture = rmqFixture;
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

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


    [Fact]
    public async Task Should_work_with_default_options()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queue = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(MessagesCount);
        var messages = MessagesFactories.Create(MessagesCount);
        await using (
            await bus.SendReceive.ReceiveAsync(queue, x => x.Add<Message>(messagesSink.Receive), cts.Token)
        )
        {
            await bus.SendReceive.SendBatchAsync(queue, messages, cts.Token);

            await messagesSink.WaitAllReceivedAsync(cts.Token);
            messagesSink.ReceivedMessages.Should().Equal(messages);
        }
    }

    [Fact(Skip ="Hang on")]
    public async Task Should_survive_restart()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var queue = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(2);
        await using (await bus.SendReceive.ReceiveAsync(queue, x => x.Add<Message>(messagesSink.Receive), cts.Token))
        {
            var message = new Message(0);
            await bus.SendReceive.SendAsync(queue, message, cts.Token);
            await rmqFixture.ManagementClient.KillAllConnectionsAsync(cts.Token);
            await bus.SendReceive.SendAsync(queue, message, cts.Token);
            await messagesSink.WaitAllReceivedAsync(cts.Token);
        }
    }
}
