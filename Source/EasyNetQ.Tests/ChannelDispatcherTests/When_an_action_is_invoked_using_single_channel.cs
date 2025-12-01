using System;
using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ChannelDispatcherTests;

public class When_an_action_is_invoked_using_single_channel : IAsyncLifetime
{
    private readonly SinglePersistentChannelDispatcher dispatcher;
    private readonly IPersistentChannelFactory channelFactory;
    private int actionResult;
    private readonly IProducerConnection producerConnection;
    private readonly Func<IChannel, Task<int>> action;

    public When_an_action_is_invoked_using_single_channel()
    {
        channelFactory = Substitute.For<IPersistentChannelFactory>();
        producerConnection = Substitute.For<IProducerConnection>();
        var consumerConnection = Substitute.For<IConsumerConnection>();
        var channel = Substitute.For<IPersistentChannel>();
        action = Substitute.For<Func<IChannel, Task<int>>>();
#pragma warning disable IDISP004
        channelFactory.CreatePersistentChannel(producerConnection, new PersistentChannelOptions()).Returns(channel);
#pragma warning restore IDISP004
        channel.InvokeChannelActionAsync(action).Returns(42);

        dispatcher = new SinglePersistentChannelDispatcher(producerConnection, consumerConnection, channelFactory);
    }

    public async Task InitializeAsync()
    {
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
