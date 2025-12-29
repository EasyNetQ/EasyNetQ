using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_declare_an_exchange_with_different_properties : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_declare_an_exchange_with_different_properties(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");

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
    public async Task Should_not_affect_correct_declares()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await bus.Advanced.ExchangeDeclareAsync("a", ExchangeType.Topic, cancellationToken: cts.Token);
        await Assert.ThrowsAsync<OperationInterruptedException>(
            () => bus.Advanced.ExchangeDeclareAsync("a", ExchangeType.Direct, cancellationToken: cts.Token)
        );
        await bus.Advanced.ExchangeDeclareAsync("a", ExchangeType.Topic, cancellationToken: cts.Token);
    }
}
