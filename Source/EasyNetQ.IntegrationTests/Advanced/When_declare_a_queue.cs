using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_declare_a_queue : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_declare_a_queue(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Should_declare_queue_with_different_modes_and_types()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queuesModes = new[] { QueueMode.Default, QueueMode.Lazy };
        foreach (var queueMode in queuesModes)
        {
            await bus.Advanced.QueueDeclareAsync(
                queue: Guid.NewGuid().ToString("N"),
                arguments: new Dictionary<string, object>().WithQueueMode(queueMode),
                cancellationToken: cts.Token
            );
        }

        var queuesTypes = new[] { QueueType.Classic, QueueType.Quorum };
        foreach (var queueType in queuesTypes)
        {
            await bus.Advanced.QueueDeclareAsync(
                queue: Guid.NewGuid().ToString("N"),
                arguments: new Dictionary<string, object>().WithQueueType(queueType),
                cancellationToken: cts.Token
            );
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }
}
