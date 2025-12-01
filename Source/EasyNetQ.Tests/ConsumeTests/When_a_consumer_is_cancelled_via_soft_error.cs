using EasyNetQ.Persistent;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_cancelled_via_soft_error : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder;

    public When_a_consumer_is_cancelled_via_soft_error()
    {
        mockBuilder = new MockBuilder();
    }

    public async Task InitializeAsync()
    {
        var queue = new Queue("my_queue", false);

#pragma warning disable IDISP004
        await mockBuilder.Bus.Advanced.ConsumeAsync(
#pragma warning restore IDISP004
            queue,
            (_, _, _) => Task.Run(() => { }),
            c => c.WithConsumerTag("consumer_tag")
        );

        mockBuilder.Consumers[0].Channel.CloseReason.Returns(
            new ShutdownEventArgs(ShutdownInitiator.Application, AmqpErrorCodes.PreconditionFailed, "Oops")
        );
        await mockBuilder.Consumers[0].HandleBasicCancelAsync("consumer_tag");
        // Wait for a periodic consumer restart
        await Task.Delay(TimeSpan.FromSeconds(10));
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public void Should_recreate_model_and_consumer()
    {
        mockBuilder.Consumers[0].Channel.Received().DisposeAsync();
        mockBuilder.Consumers[1].Channel.DidNotReceive().Dispose();
    }
}
