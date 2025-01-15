using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.SendReceive;

[Collection("RabbitMQ")]
public class When_send_receive_with_publish_confirms : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_send_receive_with_publish_confirms(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;publisherConfirms=True;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    private const int MessagesCount = 10;

    [Fact]
    public async Task Test()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var queue = Guid.NewGuid().ToString();
        var messagesSink = new MessagesSink(MessagesCount);
        var messages = MessagesFactories.Create(MessagesCount);
        using (
            await bus.SendReceive.ReceiveAsync(queue, x => x.Add<Message>(messagesSink.Receive), cts.Token)
        )
        {
            await bus.SendReceive.SendBatchAsync(queue, messages, cts.Token);

            await messagesSink.WaitAllReceivedAsync(cts.Token);
            messagesSink.ReceivedMessages.Should().Equal(messages);
        }
    }
}
