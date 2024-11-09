using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Tests.ProducerTests;

public class When_IModel_throws_because_of_closed_connection : IDisposable
{
    public When_IModel_throws_because_of_closed_connection()
    {
        mockBuilder = new MockBuilder("host=localhost;timeout=1");

        mockBuilder.NextModel
            .WhenForAnyArgs(x => x.ExchangeDeclareAsync(null, null, false, false, null))
            .Do(_ =>
            {
                var args = new ShutdownEventArgs(ShutdownInitiator.Peer, 320,
                    "CONNECTION_FORCED - Closed via management plugin");
                throw new OperationInterruptedException(args);
            });
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public void Should_try_to_reconnect_until_timeout()
    {
        Assert.Throws<TaskCanceledException>(() => mockBuilder.PubSub.Publish(new MyMessage { Text = "Hello World" }));
    }
}
