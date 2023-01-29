using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ChannelDispatcherTests;

public class When_an_action_is_invoked_that_throws_using_single_channel : IDisposable
{
    private readonly SinglePersistentChannelDispatcher dispatcher;

    public When_an_action_is_invoked_that_throws_using_single_channel()
    {
        var channelFactory = Substitute.For<IPersistentChannelFactory>();
        var producerConnection = Substitute.For<IProducerConnection>();
        var consumerConnection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IPersistentChannel>();
        var model = Substitute.For<IModel>();

        channelFactory.CreatePersistentChannel(producerConnection, new PersistentChannelOptions()).Returns(channel);
        channel.InvokeChannelActionAsync<int, FuncBasedPersistentChannelAction<int>>(default)
            .ReturnsForAnyArgs(x => ((FuncBasedPersistentChannelAction<int>)x[0]).Invoke(model));

        dispatcher = new SinglePersistentChannelDispatcher(producerConnection, consumerConnection, channelFactory);
    }

    public void Dispose()
    {
        dispatcher.Dispose();
    }

    [Fact]
    public async Task Should_raise_the_exception_on_the_calling_thread()
    {
        await Assert.ThrowsAsync<CrazyTestOnlyException>(
            () => dispatcher.InvokeAsync<int>(_ => throw new CrazyTestOnlyException(), PersistentChannelDispatchOptions.ProducerTopology).AsTask()
        );
    }

    [Fact]
    public async Task Should_call_action_when_previous_threw_an_exception()
    {
        await Assert.ThrowsAsync<Exception>(
            () => dispatcher.InvokeAsync<int>(_ => throw new Exception(), PersistentChannelDispatchOptions.ProducerTopology).AsTask()
        );

        var result = await dispatcher.InvokeAsync(_ => 42, PersistentChannelDispatchOptions.ProducerTopology);
        result.Should().Be(42);
    }

    private class CrazyTestOnlyException : Exception
    {
    }
}
