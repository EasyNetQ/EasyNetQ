using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_delete_a_queue_with_name : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_delete_a_queue_with_name(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    [Fact]
    public async Task Should_delete_existing_queue()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queueName = Guid.NewGuid().ToString();
        var queue = await bus.Advanced.QueueDeclareAsync(queue: queueName, cancellationToken: cts.Token);

        await bus.Advanced.QueueDeclarePassiveAsync(queueName, cts.Token);
        await bus.Advanced.QueueDeleteAsync(queueName, cancellationToken: cts.Token);

        var exception = Assert.Throws<OperationInterruptedException>(() => bus.Advanced.QueueDeclarePassive(queueName, cts.Token));
        Assert.Equal(404, exception.ShutdownReason.ReplyCode);
    }

    public void Dispose()
    {
        serviceProvider?.Dispose();
    }
}
