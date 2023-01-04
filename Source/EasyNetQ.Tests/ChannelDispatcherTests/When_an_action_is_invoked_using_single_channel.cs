using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ChannelDispatcherTests;

public class When_an_action_is_invoked_using_single_channel : IDisposable
{
    private readonly SinglePersistentChannelDispatcher dispatcher;
    private readonly IPersistentChannelFactory channelFactory;
    private readonly int actionResult;

    private readonly IProducerConnection producerConnection;

    public When_an_action_is_invoked_using_single_channel()
    {
        channelFactory = Substitute.For<IPersistentChannelFactory>();
        producerConnection = Substitute.For<IProducerConnection>();
        var consumerConnection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IPersistentChannel>();
        var action = Substitute.For<Func<IModel, int>>();
        channelFactory.CreatePersistentChannel(producerConnection, new PersistentChannelOptions()).Returns(channel);
        channel.InvokeChannelActionAsync(action).Returns(42);

        dispatcher = new SinglePersistentChannelDispatcher(producerConnection, consumerConnection, channelFactory);

        actionResult = dispatcher.InvokeAsync(action, PersistentChannelDispatchOptions.ProducerTopology)
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose()
    {
        dispatcher.Dispose();
    }

    [Fact]
    public void Should_create_a_persistent_channel()
    {
        channelFactory.Received().CreatePersistentChannel(producerConnection, new PersistentChannelOptions());
    }

    [Fact]
    public void Should_receive_action_result()
    {
        actionResult.Should().Be(42);
    }
}
