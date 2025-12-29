using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_connected_event_raised : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_connected_event_raised(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Test()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        using var mre = new ManualResetEventSlim(false);
        bus.Advanced.Connected += (_, _) => mre.Set();

        await bus.Advanced.ExchangeDeclareAsync(Guid.NewGuid().ToString("N"), cancellationToken: cts.Token);

        mre.Wait(cts.Token);
    }
}
