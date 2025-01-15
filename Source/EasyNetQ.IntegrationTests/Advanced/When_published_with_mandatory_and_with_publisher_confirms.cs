using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_published_with_mandatory_and_with_publisher_confirms : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_published_with_mandatory_and_with_publisher_confirms(RabbitMQFixture rmqFixture)
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
    public async Task Should_throw_message_returned_exception()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var exchange = await bus.Advanced.ExchangeDeclareAsync(
            Guid.NewGuid().ToString("N"), ExchangeType.Direct, cancellationToken: cts.Token
        );

        await Assert.ThrowsAsync<PublishReturnedException>(
            () => bus.Advanced.PublishAsync(
                exchange, "#", true, true, MessageProperties.Empty, Array.Empty<byte>(), cts.Token
            )
        );
    }
}
