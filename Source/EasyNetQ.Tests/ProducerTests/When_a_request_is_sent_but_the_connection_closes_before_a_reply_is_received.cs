using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_request_is_sent_but_the_connection_closes_before_a_reply_is_received : IDisposable
{
    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder = new();

    [Fact]
    public Task Should_throw_an_EasyNetQException()
    {
        return Assert.ThrowsAsync<EasyNetQException>(() =>
        {
            var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
            // mockBuilder.Connection.ConnectionShutdownAsync += Raise.EventWith(null, new ShutdownEventArgs(ShutdownInitiator.Application, 0, "replyText", "cause"));
            // mockBuilder.Connection.RecoverySucceededAsync += Raise.EventWith(null, EventArgs.Empty);
            return task;
        });
    }
}
