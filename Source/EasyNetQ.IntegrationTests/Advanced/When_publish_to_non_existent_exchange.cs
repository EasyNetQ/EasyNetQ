using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_publish_to_non_existent_exchange : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_to_non_existent_exchange(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

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
    public async Task Should_not_affect_publish_to_existent_exchange()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await bus.Advanced.ExchangeDeclareAsync("existent", ExchangeType.Topic, cancellationToken: cts.Token);
        await Assert.ThrowsAsync<AlreadyClosedException>(() =>
            bus.Advanced.PublishAsync(
                new Exchange("non-existent"), "#", false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
            )
        );
        await bus.Advanced.PublishAsync(
            new Exchange("existent"), "#", false, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
        );
    }
}
