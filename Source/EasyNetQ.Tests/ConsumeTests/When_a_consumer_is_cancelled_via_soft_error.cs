using EasyNetQ.Persistent;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_consumer_is_cancelled_via_soft_error : IDisposable
{
    private readonly MockBuilder mockBuilder;

    public When_a_consumer_is_cancelled_via_soft_error()
    {
        mockBuilder = new MockBuilder();

        var queue = new Queue("my_queue", false);

#pragma warning disable IDISP004
        mockBuilder.Bus.Advanced.Consume(
#pragma warning restore IDISP004
            queue,
            (_, _, _) => Task.Run(() => { }),
            c => c.WithConsumerTag("consumer_tag")
        );

        mockBuilder.Consumers[0].Channel.CloseReason.Returns(
            new ShutdownEventArgs(ShutdownInitiator.Application, AmqpErrorCodes.PreconditionFailed, "Oops")
        );
        mockBuilder.Consumers[0].HandleBasicCancelAsync("consumer_tag").GetAwaiter().GetResult();
        // Wait for a periodic consumer restart
        Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public void Should_recreate_model_and_consumer()
    {
        mockBuilder.Consumers[0].Channel.Received().Dispose();
        mockBuilder.Consumers[1].Channel.DidNotReceive().Dispose();
    }
}
