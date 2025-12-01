using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_request_is_sent_but_the_connection_closes_before_a_reply_is_received : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder = new();

    [Fact]
    public Task Should_throw_an_EasyNetQException()
    {
        return Assert.ThrowsAsync<EasyNetQException>(async () =>
        {
            AsyncEventHandler<ShutdownEventArgs> shutdownHandlers = null;
            AsyncEventHandler<AsyncEventArgs> recoveryHandlers= null;
            mockBuilder.Connection.ConnectionShutdownAsync += Arg.Do<AsyncEventHandler<ShutdownEventArgs>>(h => shutdownHandlers += h);
            mockBuilder.Connection.RecoverySucceededAsync += Arg.Do<AsyncEventHandler<AsyncEventArgs>>(h => recoveryHandlers += h);
            var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
            await shutdownHandlers?.Invoke(mockBuilder.Connection, new ShutdownEventArgs(ShutdownInitiator.Application, 0, "replyText", "cause"));
            await recoveryHandlers?.Invoke(mockBuilder.Connection, AsyncEventArgs.Empty);
            await task;
        });
    }
}
