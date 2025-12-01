using System;
using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ChannelDispatcherTests;

public class When_an_action_is_invoked_using_multi_channel : IAsyncLifetime
{
    private MultiPersistentChannelDispatcher dispatcher;
    private readonly IPersistentChannelFactory channelFactory;
    private int actionResult;
    private readonly IProducerConnection producerConnection;

    public When_an_action_is_invoked_using_multi_channel()
    {
        channelFactory = Substitute.For<IPersistentChannelFactory>();
        producerConnection = Substitute.For<IProducerConnection>();

        
    }

    public async Task InitializeAsync()
    {
        var consumerConnection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IPersistentChannel>();
        var action = Substitute.For<Func<IChannel, Task<int>>>();
#pragma warning disable IDISP004
        channelFactory.CreatePersistentChannel(producerConnection, new PersistentChannelOptions()).Returns(channel);
#pragma warning restore IDISP004
        channel.InvokeChannelActionAsync(action).Returns(42);

        dispatcher = new MultiPersistentChannelDispatcher(1, producerConnection, consumerConnection, channelFactory);
        actionResult = await dispatcher.InvokeAsync(action, PersistentChannelDispatchOptions.ProducerTopology);
    }

    public async Task DisposeAsync()
    {
        await dispatcher.DisposeAsync();
    }

    [Fact]
    public void Should_create_a_persistent_channel()
    {
#pragma warning disable IDISP004
        channelFactory.Received().CreatePersistentChannel(producerConnection, new PersistentChannelOptions());
#pragma warning restore IDISP004
    }

    [Fact]
    public void Should_receive_action_result()
    {
        actionResult.Should().Be(42);
    }
}
