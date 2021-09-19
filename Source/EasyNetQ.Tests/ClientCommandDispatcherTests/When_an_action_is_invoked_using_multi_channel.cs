// ReSharper disable InconsistentNaming

using EasyNetQ.Producer;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using System;
using EasyNetQ.Persistent;
using Xunit;

namespace EasyNetQ.Tests.ClientCommandDispatcherTests
{
    public class When_an_action_is_invoked_using_multi_channel : IDisposable
    {
        private readonly IProducerCommandDispatcher dispatcher;
        private readonly IPersistentChannelFactory channelFactory;
        private readonly int actionResult;
        private readonly IProducerConnection connection;

        public When_an_action_is_invoked_using_multi_channel()
        {
            channelFactory = Substitute.For<IPersistentChannelFactory>();
            connection = Substitute.For<IProducerConnection>();
            var channel = Substitute.For<IPersistentChannel>();
            var action = Substitute.For<Func<IModel, int>>();
            channelFactory.CreatePersistentChannel(connection, new PersistentChannelOptions()).Returns(channel);
            channel.InvokeChannelActionAsync(action).Returns(42);

            dispatcher = new MultiChannelProducerCommandDispatcher(1, connection, channelFactory);
            actionResult = dispatcher.InvokeAsync(action, ChannelDispatchOptions.Default)
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
            channelFactory.Received().CreatePersistentChannel(connection, new PersistentChannelOptions());
        }

        [Fact]
        public void Should_receive_action_result()
        {
            actionResult.Should().Be(42);
        }
    }
}

// ReSharper restore InconsistentNaming
